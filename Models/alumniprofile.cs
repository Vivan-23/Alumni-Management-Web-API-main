namespace AlumniManagementApi.Models
{
    public class AlumniProfile
    {
        public int Id { get; set; }

        // Foreign Key
        public Guid UserId { get; set; }

        // Navigation Property
        public User User { get; set; }

        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int batchYear { get; set; }
        public string degree { get; set; }
        public string currentCompany { get; set; }
        public string currentRole { get; set; }
        public string location { get; set; }
        public string LinkedinURL { get; set; }
    }
}
