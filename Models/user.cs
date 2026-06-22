namespace AlumniManagementApi.Models
{
    public class user
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public role RoleId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
