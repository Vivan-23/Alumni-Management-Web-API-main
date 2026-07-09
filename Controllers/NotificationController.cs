using AlumniManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize] // Requires authentication for all operations
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var notifications = await _notificationService.GetMyNotificationsAsync(userId);
            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var success = await _notificationService.MarkAsReadAsync(id, userId, ipAddress);

            if (!success)
            {
                return NotFound(new { message = "Notification not found." });
            }

            return Ok(new { message = "Notification marked as read successfully." });
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _notificationService.MarkAllAsReadAsync(userId, ipAddress);

            return Ok(new { message = "All notifications marked as read successfully." });
        }
    }
}
