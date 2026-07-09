namespace AlumniManagementApi.Services
{
    public interface IRabbitMQPublisher
    {
        void PublishJobPosted(int jobId, string jobTitle, string companyName);
    }
}
