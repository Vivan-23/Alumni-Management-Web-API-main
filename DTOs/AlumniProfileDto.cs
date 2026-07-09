using System;

namespace AlumniManagementApi.DTOs
{
    public class AlumniProfileDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int batchYear { get; set; }
        public string degree { get; set; } = string.Empty;
        public string currentCompany { get; set; } = string.Empty;
        public string currentRole { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
        public string LinkedinURL { get; set; } = string.Empty;
    }
}
