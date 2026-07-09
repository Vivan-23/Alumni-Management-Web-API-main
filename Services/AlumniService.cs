using AlumniManagementApi.Data.AlumniManagementApi.Data;
using AlumniManagementApi.DTOs;
using AlumniManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public class AlumniService : IAlumniService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redisConnection;

        public AlumniService(
            AppDbContext context,
            IAuditService auditService,
            IDistributedCache cache,
            IConnectionMultiplexer redisConnection)
        {
            _context = context;
            _auditService = auditService;
            _cache = cache;
            _redisConnection = redisConnection;
        }

        public async Task<IEnumerable<AlumniProfileDto>> GetAlumniAsync(int page, int pageSize, int? batchYear, string? location, string? company)
        {
            var cacheKey = $"alumni:list:page_{page}_size_{pageSize}_by_{batchYear ?? 0}_loc_{location ?? "all"}_comp_{company ?? "all"}";

            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    return JsonSerializer.Deserialize<List<AlumniProfileDto>>(cachedData) ?? new List<AlumniProfileDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis read failed: {ex.Message}");
            }

            var query = _context.AlumniProfiles.AsQueryable();

            if (batchYear.HasValue)
            {
                query = query.Where(a => a.batchYear == batchYear.Value);
            }
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(a => a.location.Contains(location));
            }
            if (!string.IsNullOrEmpty(company))
            {
                query = query.Where(a => a.currentCompany.Contains(company));
            }

            var profiles = await query
                .OrderBy(a => a.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AlumniProfileDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Name = a.Name,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    batchYear = a.batchYear,
                    degree = a.degree,
                    currentCompany = a.currentCompany,
                    currentRole = a.currentRole,
                    location = a.location,
                    LinkedinURL = a.LinkedinURL
                })
                .ToListAsync();

            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(profiles), options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis write failed: {ex.Message}");
            }

            return profiles;
        }

        public async Task<AlumniProfileDto?> GetAlumniByIdAsync(Guid id)
        {
            var a = await _context.AlumniProfiles.FirstOrDefaultAsync(ap => ap.UserId == id);
            if (a == null) return null;

            return new AlumniProfileDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Name = a.Name,
                Email = a.Email,
                PhoneNumber = a.PhoneNumber,
                batchYear = a.batchYear,
                degree = a.degree,
                currentCompany = a.currentCompany,
                currentRole = a.currentRole,
                location = a.location,
                LinkedinURL = a.LinkedinURL
            };
        }

        public async Task<AlumniProfileDto?> UpdateAlumniProfileAsync(Guid id, AlumniProfileDto dto, Guid performingUserId, string? ipAddress = null)
        {
            if (id != performingUserId)
            {
                // Own profile only check
                return null;
            }

            var profile = await _context.AlumniProfiles.FirstOrDefaultAsync(ap => ap.UserId == id);
            if (profile == null)
            {
                return null;
            }

            profile.Name = dto.Name;
            profile.Email = dto.Email;
            profile.PhoneNumber = dto.PhoneNumber;
            profile.batchYear = dto.batchYear;
            profile.degree = dto.degree;
            profile.currentCompany = dto.currentCompany;
            profile.currentRole = dto.currentRole;
            profile.location = dto.location;
            profile.LinkedinURL = dto.LinkedinURL;

            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidateAlumniCacheAsync();

            // Write Audit Log
            await _auditService.LogAsync(
                "AlumniProfile.Update",
                "AlumniProfile",
                profile.Id.ToString(),
                performingUserId,
                ipAddress,
                $"Updated own alumni profile for user: {id}"
            );

            return new AlumniProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Name = profile.Name,
                Email = profile.Email,
                PhoneNumber = profile.PhoneNumber,
                batchYear = profile.batchYear,
                degree = profile.degree,
                currentCompany = profile.currentCompany,
                currentRole = profile.currentRole,
                location = profile.location,
                LinkedinURL = profile.LinkedinURL
            };
        }

        public async Task<IEnumerable<AlumniProfileDto>> SearchAlumniAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<AlumniProfileDto>();
            }

            var lowerQuery = query.ToLower();

            var results = await _context.AlumniProfiles
                .Where(a => a.Name.ToLower().Contains(lowerQuery) ||
                            a.Email.ToLower().Contains(lowerQuery) ||
                            a.degree.ToLower().Contains(lowerQuery) ||
                            a.currentCompany.ToLower().Contains(lowerQuery) ||
                            a.location.ToLower().Contains(lowerQuery))
                .Select(a => new AlumniProfileDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Name = a.Name,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    batchYear = a.batchYear,
                    degree = a.degree,
                    currentCompany = a.currentCompany,
                    currentRole = a.currentRole,
                    location = a.location,
                    LinkedinURL = a.LinkedinURL
                })
                .ToListAsync();

            return results;
        }

        private async Task InvalidateAlumniCacheAsync()
        {
            try
            {
                var endpoints = _redisConnection.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _redisConnection.GetServer(endpoint);
                    var keys = server.Keys(pattern: "alumni:list:*").ToArray();
                    foreach (var key in keys)
                    {
                        await _cache.RemoveAsync(key.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis cache invalidation failed: {ex.Message}");
            }
        }
    }
}
