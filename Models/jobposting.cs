namespace AlumniManagementApi.Models
{
    public class jobposting
    {
        public int Id { get; set; }
        public Guid UserId{ get; set; }
        public string JobTitle { get; set; }
        public string JobDescription { get; set; }
        public string CompanyName { get; set; }
        public string Location { get; set; }
        public DateTime PostedDate { get; set; }
        public DateTime ApplicationDeadline { get; set; } 
        public string applyUrl { get; set; }
        public bool IsActive { get; set; }

    }
}
