namespace AlumniManagementApi.Models
{
    public enum Status
    {
        Going,
        Interested,
        Declined
    }
    public class EventRSVP
    {
        public int EventId { get; set; }
        public Event Event { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public Status RsvpStatus { get; set; }
    }
}
