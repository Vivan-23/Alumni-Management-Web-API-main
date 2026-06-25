using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires any authenticated user
    public class NotificationController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetNotifications()
        {
            return Ok(new[]
            {
                new { Id = 1, Title = "New Job Alert", Message = "A new job matching your profile has been posted.", IsRead = false },
                new { Id = 2, Title = "Event Reminder", Message = "Don't forget the Alumni Meetup tomorrow!", IsRead = true }
            });
        }
    }
}
