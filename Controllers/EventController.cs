using AlumniManagementApi.DTOs;
using AlumniManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize] // Requires authentication for all operations
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _eventService.GetEventsAsync();
            return Ok(events);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _eventService.GetEventByIdAsync(id);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }
            return Ok(ev);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Admin only
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var createdEvent = await _eventService.CreateEventAsync(dto, userId, ipAddress);

            return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, createdEvent);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Admin only
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] CreateEventDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var updatedEvent = await _eventService.UpdateEventAsync(id, dto, userId, ipAddress);

            if (updatedEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            return Ok(updatedEvent);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Admin only
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var success = await _eventService.DeleteEventAsync(id, userId, ipAddress);

            if (!success)
            {
                return NotFound(new { message = "Event not found." });
            }

            return Ok(new { message = "Event deleted successfully." });
        }

        [HttpPost("{id}/rsvp")]
        public async Task<IActionResult> CreateOrUpdateRSVP(int id, [FromBody] CreateEventRSVPDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var rsvp = await _eventService.CreateOrUpdateRSVPAsync(id, userId, dto.RsvpStatus, ipAddress);

            if (rsvp == null)
            {
                return BadRequest(new { message = "Invalid event ID or RSVP status. Allowed statuses: Going, Interested, Declined." });
            }

            return Ok(rsvp);
        }

        [HttpGet("{id}/rsvp")]
        public async Task<IActionResult> GetOwnRSVPStatus(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var rsvp = await _eventService.GetRSVPStatusAsync(id, userId);
            if (rsvp == null)
            {
                return NotFound(new { message = "RSVP status not found for this event." });
            }

            return Ok(rsvp);
        }
    }
}
