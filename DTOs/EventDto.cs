using System;

namespace AlumniManagementApi.DTOs
{
    public class EventDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
    }

    public class CreateEventDto
    {
        public string EventName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
    }

    public class EventRSVPDto
    {
        public int EventId { get; set; }
        public Guid UserId { get; set; }
        public string RsvpStatus { get; set; } = string.Empty;
    }

    public class CreateEventRSVPDto
    {
        public string RsvpStatus { get; set; } = string.Empty; // Going, Interested, Declined
    }
}
