using AlumniManagementApi.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface IDonationService
    {
        Task<CreateOrderResponseDto> CreateOrderAsync(CreateOrderRequestDto dto, Guid userId, string? ipAddress = null);
        Task<bool> ProcessWebhookAsync(string rawBody, string headerSignature, string? ipAddress = null);
        Task<IEnumerable<DonationDto>> GetMyDonationHistoryAsync(Guid userId);
        Task<bool> VerifyPaymentAsync(VerifyPaymentRequestDto dto, Guid userId, string? ipAddress = null);
    }
}
