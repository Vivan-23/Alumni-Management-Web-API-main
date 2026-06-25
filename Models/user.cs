namespace AlumniManagementApi.Models
{
    public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    
    public int RoleId { get; set; } // Separate FK property
    public Role Role { get; set; }   // Navigation property
    
    public DateTime CreatedAt { get; set; }

    // Navigation Collections
    public ICollection<Donation> Donations { get; set; }
    public ICollection<JobPosting> JobPostings { get; set; }
    public ICollection<Event> Events { get; set; }
    public ICollection<EventRSVP> RSVPs { get; set; }
    public ICollection<Notification> Notifications { get; set; }
}

}
