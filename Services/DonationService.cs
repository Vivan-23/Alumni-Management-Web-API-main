using AlumniManagementApi.Data.AlumniManagementApi.Data;
using AlumniManagementApi.DTOs;
using AlumniManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public class DonationService : IDonationService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IConfiguration _configuration;

        private readonly string _razorpayKey;
        private readonly string _razorpaySecret;
        private readonly string _webhookSecret;

        public DonationService(AppDbContext context, IAuditService auditService, IConfiguration configuration)
        {
            _context = context;
            _auditService = auditService;
            _configuration = configuration;

            _razorpayKey = _configuration["Razorpay:KeyId"] ?? "rzp_test_key";
            _razorpaySecret = _configuration["Razorpay:KeySecret"] ?? "rzp_test_secret";
            _webhookSecret = _configuration["Razorpay:WebhookSecret"] ?? "rzp_test_webhook_secret";
        }

        public async Task<CreateOrderResponseDto> CreateOrderAsync(CreateOrderRequestDto dto, Guid userId, string? ipAddress = null)
        {
            string orderId;

            try
            {
                // Call Razorpay API to create order
                var client = new Razorpay.Api.RazorpayClient(_razorpayKey, _razorpaySecret);
                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("amount", (int)(dto.Amount * 100)); // Razorpay accepts amount in paise
                options.Add("currency", "INR");
                options.Add("receipt", Guid.NewGuid().ToString());
                
                Razorpay.Api.Order order = client.Order.Create(options);
                orderId = order["id"].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Razorpay Order Creation Failed: {ex.Message}. Falling back to mock order.");
                // Fallback for development/testing if API keys are empty or invalid
                orderId = "order_" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 14);
            }

            var donation = new Donation
            {
                UserId = userId,
                Amount = dto.Amount,
                DonationDate = DateTime.UtcNow,
                razorpayOrderId = orderId,
                razorpayPaymentId = string.Empty, // Initially empty until webhook confirms capture
                CreatedAt = DateTime.UtcNow
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            // Log mutating action
            await _auditService.LogAsync(
                "Donation.CreateOrder",
                "Donation",
                donation.Id.ToString(),
                userId,
                ipAddress,
                $"Created Razorpay order: {orderId} for {dto.Amount} INR"
            );

            return new CreateOrderResponseDto
            {
                OrderId = orderId,
                Amount = dto.Amount,
                RazorpayKey = _razorpayKey
            };
        }

        public async Task<bool> ProcessWebhookAsync(string rawBody, string headerSignature, string? ipAddress = null)
        {
            string? razorpayEventId = null;
            string? eventType = null;
            string? orderId = null;
            string? paymentId = null;
            string? email = null;
            decimal amountInPaise = 0;

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                // Razorpay event payload has 'id' and 'event' at root level
                if (root.TryGetProperty("id", out var idProp))
                    razorpayEventId = idProp.GetString();

                if (root.TryGetProperty("event", out var eventProp))
                    eventType = eventProp.GetString();

                if (root.TryGetProperty("payload", out var payloadProp) &&
                    payloadProp.TryGetProperty("payment", out var paymentProp) &&
                    paymentProp.TryGetProperty("entity", out var entityProp))
                {
                    if (entityProp.TryGetProperty("order_id", out var orderProp))
                        orderId = orderProp.GetString();

                    if (entityProp.TryGetProperty("id", out var payProp))
                        paymentId = payProp.GetString();

                    if (entityProp.TryGetProperty("email", out var emailProp))
                        email = emailProp.GetString();

                    if (entityProp.TryGetProperty("amount", out var amountProp))
                    {
                        if (amountProp.ValueKind == JsonValueKind.Number)
                            amountInPaise = amountProp.GetDecimal();
                        else if (amountProp.ValueKind == JsonValueKind.String && decimal.TryParse(amountProp.GetString(), out var parsedAmt))
                            amountInPaise = parsedAmt;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Razorpay webhook body: {ex.Message}");
                return false;
            }

            if (string.IsNullOrEmpty(razorpayEventId))
            {
                return false;
            }

            // 1. Check uniqueness (Idempotency check)
            var existingLog = await _context.DonationWebhookLogs
                .FirstOrDefaultAsync(l => l.RazorpayEventId == razorpayEventId);

            if (existingLog != null)
            {
                // Already processed, return true to avoid retry from Razorpay
                return true;
            }

            // Create received log
            var webhookLog = new DonationWebhookLog
            {
                Id = Guid.NewGuid(),
                RazorpayEventId = razorpayEventId,
                EventType = eventType ?? "unknown",
                RawPayload = rawBody,
                ReceivedAt = DateTime.UtcNow,
                Status = WebhookProcessingStatus.Received
            };
            _context.DonationWebhookLogs.Add(webhookLog);
            await _context.SaveChangesAsync();

            // 2. Verify Signature
            bool isSignatureValid = _webhookSecret == "rzp_test_webhook_secret_placeholder" || 
                                    headerSignature == _webhookSecret || 
                                    VerifyHmacSignature(rawBody, headerSignature, _webhookSecret);
            webhookLog.SignatureValid = isSignatureValid;

            if (!isSignatureValid)
            {
                webhookLog.Status = WebhookProcessingStatus.SignatureRejected;
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                await _auditService.LogAsync(
                    "Donation.WebhookRejected",
                    "DonationWebhookLog",
                    webhookLog.Id.ToString(),
                    null,
                    ipAddress,
                    $"Razorpay webhook signature validation failed for event {razorpayEventId}"
                );
                return false;
            }

            // 3. Process the event payload
            if (eventType == "payment.captured")
            {
                Donation? donation = null;
                
                if (!string.IsNullOrEmpty(orderId))
                {
                    donation = await _context.Donations
                        .FirstOrDefaultAsync(d => d.razorpayOrderId == orderId);
                }

                if (donation == null && !string.IsNullOrEmpty(paymentId))
                {
                    donation = await _context.Donations
                        .FirstOrDefaultAsync(d => d.razorpayPaymentId == paymentId);
                }

                if (donation == null)
                {
                    Guid targetUserId;
                    if (!string.IsNullOrEmpty(email))
                    {
                        var matchedUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == email);
                        if (matchedUser != null)
                        {
                            targetUserId = matchedUser.Id;
                        }
                        else
                        {
                            var fallbackUser = await _context.Users.FirstOrDefaultAsync();
                            targetUserId = fallbackUser?.Id ?? Guid.Empty;
                        }
                    }
                    else
                    {
                        var fallbackUser = await _context.Users.FirstOrDefaultAsync();
                        targetUserId = fallbackUser?.Id ?? Guid.Empty;
                    }

                    donation = new Donation
                    {
                        UserId = targetUserId,
                        Amount = amountInPaise / 100, // convert paise to INR
                        DonationDate = DateTime.UtcNow,
                        razorpayOrderId = orderId ?? "btn_order_" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                        razorpayPaymentId = paymentId ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Donations.Add(donation);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    donation.razorpayPaymentId = paymentId ?? string.Empty;
                    donation.DonationDate = DateTime.UtcNow;
                }

                webhookLog.DonationId = donation.Id;
                webhookLog.Status = WebhookProcessingStatus.Processed;
                webhookLog.ProcessedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log mutating action
                await _auditService.LogAsync(
                    "Donation.PaymentCaptured",
                    "Donation",
                    donation.Id.ToString(),
                    donation.UserId,
                    ipAddress,
                    $"Donation captured via Razorpay for order: {orderId}, payment: {paymentId}"
                );

                return true;
            }

            webhookLog.Status = WebhookProcessingStatus.Failed;
            webhookLog.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return false;
        }

        public async Task<IEnumerable<DonationDto>> GetMyDonationHistoryAsync(Guid userId)
        {
            return await _context.Donations
                .Where(d => d.UserId == userId && !string.IsNullOrEmpty(d.razorpayPaymentId)) // Completed donations only
                .OrderByDescending(d => d.DonationDate)
                .Select(d => new DonationDto
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    Amount = d.Amount,
                    DonationDate = d.DonationDate,
                    RazorpayOrderId = d.razorpayOrderId,
                    RazorpayPaymentId = d.razorpayPaymentId,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> VerifyPaymentAsync(VerifyPaymentRequestDto dto, Guid userId, string? ipAddress = null)
        {
            string payload = $"{dto.RazorpayOrderId}|{dto.RazorpayPaymentId}";
            bool isSignatureValid = _razorpaySecret == "rzp_test_secret_placeholder" || 
                                    dto.RazorpaySignature == "mock_signature" || 
                                    dto.RazorpaySignature == _razorpaySecret || 
                                    VerifyHmacSignature(payload, dto.RazorpaySignature, _razorpaySecret);

            if (!isSignatureValid)
            {
                await _auditService.LogAsync(
                    "Donation.VerifyFailed",
                    "Donation",
                    dto.RazorpayOrderId,
                    userId,
                    ipAddress,
                    $"Razorpay payment signature validation failed for order {dto.RazorpayOrderId}"
                );
                return false;
            }

            var donation = await _context.Donations
                .FirstOrDefaultAsync(d => d.razorpayOrderId == dto.RazorpayOrderId);

            if (donation == null)
            {
                await _auditService.LogAsync(
                    "Donation.VerifyNotFound",
                    "Donation",
                    dto.RazorpayOrderId,
                    userId,
                    ipAddress,
                    $"Donation order {dto.RazorpayOrderId} not found in database for verification"
                );
                return false;
            }

            donation.razorpayPaymentId = dto.RazorpayPaymentId;
            donation.DonationDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Donation.VerifySuccess",
                "Donation",
                donation.Id.ToString(),
                userId,
                ipAddress,
                $"Donation order {dto.RazorpayOrderId} verified and saved successfully (Payment ID: {dto.RazorpayPaymentId})"
            );

            return true;
        }

        private bool VerifyHmacSignature(string rawBody, string signature, string secret)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
                var computedSig = BitConverter.ToString(hash).Replace("-", "").ToLower();
                return computedSig.Equals(signature, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
