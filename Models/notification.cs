namespace AlumniManagementApi.Models
{
    public enum Type
    {
        NewJob,
        EventReminder,
        DonationReceipt
    }
    public class notification
    {
        public int Id { get; set; }
        public Guid UserId {  get; set; }
        public string Title { get; set; }
        public Type Type { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
