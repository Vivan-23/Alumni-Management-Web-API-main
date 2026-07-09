using AlumniManagementApi.Data.AlumniManagementApi.Data;
using AlumniManagementApi.DTOs;
using AlumniManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService _auditService;

        public UserService(AppDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IEnumerable<UserDto>> GetUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();
            var profiles = await _context.AlumniProfiles
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.Name);

            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleName = u.Role?.RoleName ?? string.Empty,
                Name = profiles.TryGetValue(u.Id, out var name) ? name : string.Empty,
                CreatedAt = u.CreatedAt
            });
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return null;

            var profile = await _context.AlumniProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName ?? string.Empty,
                Name = profile?.Name ?? string.Empty,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> UpdateUserRoleAsync(string email, UpdateRoleRequest request, Guid? performingUserId, string? ipAddress = null)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return false;

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null) return false;

            var oldRoleName = user.Role?.RoleName ?? "Unknown";
            user.RoleId = request.RoleId;

            await _context.SaveChangesAsync();

            // Log mutating action
            await _auditService.LogAsync(
                "User.UpdateRole",
                "User",
                user.Id.ToString(),
                performingUserId,
                ipAddress,
                $"Updated user {user.Email} role from {oldRoleName} to {role.RoleName}."
            );

            return true;
        }

        public async Task<bool> DeleteUserAsync(string email, Guid? performingUserId, string? ipAddress = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            // 1. Nullify AuditLogs references
            var auditLogs = await _context.AuditLogs.Where(a => a.UserId == user.Id).ToListAsync();
            foreach (var log in auditLogs)
            {
                log.UserId = null;
            }

            // 2. Delete Notifications
            var notifications = await _context.Notifications.Where(n => n.UserId == user.Id).ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            // 3. Delete JobPostings
            var jobPostings = await _context.JobPostings.Where(j => j.UserId == user.Id).ToListAsync();
            _context.JobPostings.RemoveRange(jobPostings);

            // 4. Delete EventRSVPs
            var rsvps = await _context.EventRSVPs.Where(r => r.UserId == user.Id).ToListAsync();
            _context.EventRSVPs.RemoveRange(rsvps);

            // 5. Delete Events (and RSVPs for those events)
            var events = await _context.Events.Where(e => e.UserId == user.Id).ToListAsync();
            foreach (var ev in events)
            {
                var eventRsvps = await _context.EventRSVPs.Where(r => r.EventId == ev.Id).ToListAsync();
                _context.EventRSVPs.RemoveRange(eventRsvps);
            }
            _context.Events.RemoveRange(events);

            // 6. Delete Donations (and DonationWebhookLogs for those donations)
            var donations = await _context.Donations.Where(d => d.UserId == user.Id).ToListAsync();
            foreach (var donation in donations)
            {
                var webhookLogs = await _context.DonationWebhookLogs.Where(w => w.DonationId == donation.Id).ToListAsync();
                _context.DonationWebhookLogs.RemoveRange(webhookLogs);
            }
            _context.Donations.RemoveRange(donations);

            // Save child changes first
            await _context.SaveChangesAsync();

            // 7. Also delete their profile if it exists
            var profile = await _context.AlumniProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                _context.AlumniProfiles.Remove(profile);
            }

            // 8. Delete the user
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            // Log mutating action
            await _auditService.LogAsync(
                "User.Delete",
                "User",
                user.Id.ToString(),
                performingUserId,
                ipAddress,
                $"Deleted user {user.Email} and their profile."
            );

            return true;
        }
    }
}
