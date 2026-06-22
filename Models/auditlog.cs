namespace AlumniManagementApi.Models
{
    public class auditlog
    {
        public Guid Id { get; set; }

        
        public Guid? UserId { get; set; }
        public user? User { get; set; }

        public string Action { get; set; } = string.Empty;     // e.g. "JobPosting.Created", "Donation.StatusChanged"
        public string EntityType { get; set; } = string.Empty; // e.g. "JobPosting"
        public string EntityId { get; set; } = string.Empty;   // store as string so it works across entity types

        public string? Details { get; set; } // JSON blob — what changed (old/new values), optional

        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
