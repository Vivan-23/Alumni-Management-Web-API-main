using AlumniManagementApi.Data.AlumniManagementApi.Data;
using AlumniManagementApi.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private const string CacheKey = "dashboard:stats";

        public DashboardService(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(CacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    return JsonSerializer.Deserialize<DashboardStatsDto>(cachedData) ?? new DashboardStatsDto();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading dashboard cache from Redis: {ex.Message}");
            }

            // Cache miss: compute and cache
            return await RecomputeAndCacheStatsAsync();
        }

        public async Task<DashboardStatsDto> RecomputeAndCacheStatsAsync()
        {
            // 1. Total Alumni Count (users with RoleName = "Alumni" or RoleId = 2)
            var totalAlumni = await _context.AlumniProfiles
                .Include(ap => ap.User)
                .CountAsync(ap => ap.User.RoleId == 2);

            // 2. Alumni by Batch Year
            var batchYearsList = await _context.AlumniProfiles
                .Include(ap => ap.User)
                .Where(ap => ap.User.RoleId == 2 && ap.batchYear > 0)
                .GroupBy(ap => ap.batchYear)
                .Select(g => new { BatchYear = g.Key, Count = g.Count() })
                .ToListAsync();

            var alumniByBatchYear = batchYearsList.ToDictionary(x => x.BatchYear, x => x.Count);

            // 3. Total Donations Amount (completed donations where razorpayPaymentId is not empty)
            var totalDonations = await _context.Donations
                .Where(d => !string.IsNullOrEmpty(d.razorpayPaymentId))
                .SumAsync(d => d.Amount);

            // 4. Active Job Count
            var activeJobs = await _context.JobPostings
                .CountAsync(j => j.IsActive);

            // 5. Upcoming Events Count
            var upcomingEvents = await _context.Events
                .CountAsync(e => e.EventDate >= DateTime.UtcNow);

            var stats = new DashboardStatsDto
            {
                TotalAlumniCount = totalAlumni,
                AlumniByBatchYear = alumniByBatchYear,
                TotalDonationsAmount = totalDonations,
                ActiveJobCount = activeJobs,
                UpcomingEventsCount = upcomingEvents
            };

            // Cache in Redis with 10-minute TTL
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(stats), options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing dashboard cache to Redis: {ex.Message}");
            }

            return stats;
        }
    }
}
