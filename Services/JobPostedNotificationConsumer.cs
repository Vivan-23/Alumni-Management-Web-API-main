using AlumniManagementApi.Data.AlumniManagementApi.Data;
using AlumniManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public class JobPostedNotificationConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IModel? _channel;
        private string? _queueName;

        public JobPostedNotificationConsumer(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
                var factory = new ConnectionFactory() { HostName = hostName };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare the exchange
                _channel.ExchangeDeclare(exchange: "job.posted", type: ExchangeType.Fanout, durable: true);

                // Declare a queue and bind it
                _queueName = _channel.QueueDeclare(queue: "alumni.job.notifications", durable: true, exclusive: false, autoDelete: false).QueueName;
                _channel.QueueBind(queue: _queueName, exchange: "job.posted", routingKey: string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ Consumer Initialization Failed: {ex.Message}");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null || string.IsNullOrEmpty(_queueName))
            {
                Console.WriteLine("RabbitMQ Consumer channel/queue is not ready. Background consumer is inactive.");
                return Task.CompletedTask;
            }

            stoppingToken.Register(() =>
            {
                try
                {
                    _channel?.Close();
                    _connection?.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RabbitMQ Consumer shutdown error: {ex.Message}");
                }
            });

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var jobEvent = JsonSerializer.Deserialize<JobEventPayload>(message);

                    if (jobEvent != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            // Find all users who are Alumni (RoleId = 2)
                            var alumniUsers = await context.Users
                                .Where(u => u.RoleId == 2)
                                .ToListAsync(stoppingToken);

                            foreach (var user in alumniUsers)
                            {
                                var notification = new Notification
                                {
                                    UserId = user.Id,
                                    Title = "New Job Posted",
                                    Type = Models.Type.NewJob,
                                    Message = $"A new job '{jobEvent.Title}' at '{jobEvent.Company}' has been posted.",
                                    CreatedAt = DateTime.UtcNow,
                                    IsRead = false
                                };
                                context.Notifications.Add(notification);
                            }

                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing job posted notification: {ex.Message}");
                    // Reject the message and requeue if transient, or just don't ack
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }

        private class JobEventPayload
        {
            public int JobId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Company { get; set; } = string.Empty;
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch
            {
                // Ignored
            }
            base.Dispose();
        }
    }
}
