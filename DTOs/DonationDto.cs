using System;

namespace AlumniManagementApi.DTOs
{
    public class DonationDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime DonationDate { get; set; }
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateOrderRequestDto
    {
        public decimal Amount { get; set; } // Amount in INR
    }

    public class CreateOrderResponseDto
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string RazorpayKey { get; set; } = string.Empty;
    }

    public class VerifyPaymentRequestDto
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
    }
}
