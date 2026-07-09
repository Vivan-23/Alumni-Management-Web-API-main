using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public class DashboardCachingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5);

        public DashboardCachingWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait slightly during startup to let services initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
                        await dashboardService.RecomputeAndCacheStatsAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in DashboardCachingWorker: {ex.Message}");
                }

                await Task.Delay(_period, stoppingToken);
            }
        }
    }
}
