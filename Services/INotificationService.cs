using AlumniManagementApi.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(Guid userId);
        Task<bool> MarkAsReadAsync(int id, Guid userId, string? ipAddress = null);
        Task<bool> MarkAllAsReadAsync(Guid userId, string? ipAddress = null);
    }
}
