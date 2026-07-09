using AlumniManagementApi.DTOs;
using AlumniManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/donations")]
    [Authorize] // Default authorization
    public class DonationController : ControllerBase
    {
        private readonly IDonationService _donationService;

        public DonationController(IDonationService donationService)
        {
            _donationService = donationService;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            if (dto.Amount <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than zero." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _donationService.CreateOrderAsync(dto, userId, ipAddress);
            return Ok(response);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequestDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            if (string.IsNullOrEmpty(dto.RazorpayOrderId) || 
                string.IsNullOrEmpty(dto.RazorpayPaymentId) || 
                string.IsNullOrEmpty(dto.RazorpaySignature))
            {
                return BadRequest(new { message = "Missing verification details." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var success = await _donationService.VerifyPaymentAsync(dto, userId, ipAddress);

            if (!success)
            {
                return BadRequest(new { message = "Payment signature verification failed." });
            }

            return Ok(new { message = "Payment verified and saved successfully." });
        }

        [HttpPost("webhook")]
        [AllowAnonymous] // Webhook is called by Razorpay servers without JWT auth
        public async Task<IActionResult> Webhook()
        {
            var signature = Request.Headers["X-Razorpay-Signature"].ToString();
            if (string.IsNullOrEmpty(signature))
            {
                return BadRequest(new { message = "Missing signature header." });
            }

            // Read raw body
            string rawBody;
            using (var reader = new StreamReader(Request.Body))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var success = await _donationService.ProcessWebhookAsync(rawBody, signature, ipAddress);

            if (!success)
            {
                // Return bad request to tell Razorpay the validation failed
                return BadRequest(new { message = "Webhook validation or processing failed." });
            }

            return Ok(new { message = "Webhook processed successfully." });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyDonationHistory()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var donations = await _donationService.GetMyDonationHistoryAsync(userId);
            return Ok(donations);
        }
    }
}
