namespace AlumniManagementApi.Models
{
    public class DonationWebhookLog
    {
        public Guid Id { get; set; }

        // Nullable: a webhook can arrive for an order you don't recognize (replay, tampering, race condition)
        public Guid? DonationId { get; set; }
        public donation? Donation { get; set; }

        // Razorpay sends this — use it as your idempotency key (unique index)
        public string RazorpayEventId { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty; // e.g. "payment.captured", "payment.failed"

        public string RawPayload { get; set; } = string.Empty; // store the full JSON body, untouched

        public bool SignatureValid { get; set; }

        public WebhookProcessingStatus Status { get; set; } = WebhookProcessingStatus.Received;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }

    public enum WebhookProcessingStatus
    {
        Received,
        SignatureRejected,
        Processed,
        Failed
    }
}
