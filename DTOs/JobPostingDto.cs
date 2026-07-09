namespace AlumniManagementApi.DTOs
{
    public class JobPostingDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime PostedDate { get; set; }
        public DateTime ApplicationDeadline { get; set; }
        public string ApplyUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateJobPostingDto
    {
        public string JobTitle { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime ApplicationDeadline { get; set; }
        public string ApplyUrl { get; set; } = string.Empty;
    }
}
