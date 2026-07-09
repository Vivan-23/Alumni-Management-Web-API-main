using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;

namespace AlumniManagementApi.Services
{
    public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
    {
        private readonly IConnection? _connection;
        private readonly IModel? _channel;

        public RabbitMQPublisher(IConfiguration configuration)
        {
            try
            {
                var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
                var factory = new ConnectionFactory() { HostName = hostName };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare a fanout exchange for publishing job postings
                _channel.ExchangeDeclare(exchange: "job.posted", type: ExchangeType.Fanout, durable: true);
            }
            catch (Exception ex)
            {
                // In case RabbitMQ is not running during startup, log but do not crash the app
                Console.WriteLine($"RabbitMQ Startup Connection Failed: {ex.Message}");
            }
        }

        public void PublishJobPosted(int jobId, string jobTitle, string companyName)
        {
            if (_channel == null)
            {
                Console.WriteLine("RabbitMQ channel is not available. Skipping message publish.");
                return;
            }

            var messageObj = new { JobId = jobId, Title = jobTitle, Company = companyName };
            var messageJson = JsonSerializer.Serialize(messageObj);
            var body = Encoding.UTF8.GetBytes(messageJson);

            _channel.BasicPublish(
                exchange: "job.posted",
                routingKey: string.Empty,
                basicProperties: null,
                body: body
            );
        }

        public void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch
            {
                // Ignored during shutdown
            }
        }
    }
}
