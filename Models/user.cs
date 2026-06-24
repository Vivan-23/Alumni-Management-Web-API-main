namespace AlumniManagementApi.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public Role RoleId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
