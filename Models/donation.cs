namespace AlumniManagementApi.Models
{
    public class Donation
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime DonationDate { get; set; }
        public string razorpayOrderId {  get; set; }
        public string razorpayPaymentId{ get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
