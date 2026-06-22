namespace AlumniManagementApi.Models
{
    public enum Status
    {
        Going,
        Interested,
        Declined
    }
    public class eventrsvp
    {
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public Status RsvpStatus { get; set; }
    }
}
