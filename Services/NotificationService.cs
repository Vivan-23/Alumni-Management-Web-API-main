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
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService _auditService;

        public NotificationService(AppDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Type = n.Type.ToString(),
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                })
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(int id, Guid userId, string? ipAddress = null)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null) return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "Notification.MarkAsRead",
                    "Notification",
                    id.ToString(),
                    userId,
                    ipAddress,
                    $"Notification marked as read: {id}"
                );
            }

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId, string? ipAddress = null)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var n in unreadNotifications)
                {
                    n.IsRead = true;
                }
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "Notification.MarkAllAsRead",
                    "Notification",
                    "All",
                    userId,
                    ipAddress,
                    $"All notifications marked as read for user: {userId}"
                );
            }

            return true;
        }
    }
}
