namespace AlumniManagementApi.Models
{
    public class @event
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string EventName { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
    }
}
