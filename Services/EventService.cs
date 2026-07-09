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
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService _auditService;

        public EventService(AppDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IEnumerable<EventDto>> GetEventsAsync()
        {
            return await _context.Events
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Location = e.Location
                })
                .ToListAsync();
        }

        public async Task<EventDto?> GetEventByIdAsync(int id)
        {
            var e = await _context.Events.FindAsync(id);
            if (e == null) return null;

            return new EventDto
            {
                Id = e.Id,
                UserId = e.UserId,
                EventName = e.EventName,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location
            };
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto dto, Guid performingUserId, string? ipAddress = null)
        {
            var ev = new @Event
            {
                UserId = performingUserId,
                EventName = dto.EventName,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                "Event.Create",
                "Event",
                ev.Id.ToString(),
                performingUserId,
                ipAddress,
                $"Created event: {ev.EventName}"
            );

            return new EventDto
            {
                Id = ev.Id,
                UserId = ev.UserId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location
            };
        }

        public async Task<EventDto?> UpdateEventAsync(int id, CreateEventDto dto, Guid performingUserId, string? ipAddress = null)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return null;

            ev.EventName = dto.EventName;
            ev.Description = dto.Description;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                "Event.Update",
                "Event",
                ev.Id.ToString(),
                performingUserId,
                ipAddress,
                $"Updated event: {ev.EventName}"
            );

            return new EventDto
            {
                Id = ev.Id,
                UserId = ev.UserId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location
            };
        }

        public async Task<bool> DeleteEventAsync(int id, Guid performingUserId, string? ipAddress = null)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return false;

            // Remove associated RSVPs first (since composite key contains EventId)
            var rsvps = await _context.EventRSVPs.Where(r => r.EventId == id).ToListAsync();
            _context.EventRSVPs.RemoveRange(rsvps);

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                "Event.Delete",
                "Event",
                id.ToString(),
                performingUserId,
                ipAddress,
                $"Deleted event: {ev.EventName}"
            );

            return true;
        }

        public async Task<EventRSVPDto?> CreateOrUpdateRSVPAsync(int eventId, Guid userId, string rsvpStatus, string? ipAddress = null)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return null;

            if (!Enum.TryParse<Models.Status>(rsvpStatus, true, out var statusEnum))
            {
                return null; // Invalid status string
            }

            var rsvp = await _context.EventRSVPs
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (rsvp == null)
            {
                rsvp = new EventRSVP
                {
                    EventId = eventId,
                    UserId = userId,
                    RsvpStatus = statusEnum
                };
                _context.EventRSVPs.Add(rsvp);
            }
            else
            {
                rsvp.RsvpStatus = statusEnum;
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                "EventRSVP.CreateOrUpdate",
                "EventRSVP",
                $"{eventId}_{userId}",
                userId,
                ipAddress,
                $"RSVP set to {statusEnum} for event: {ev.EventName}"
            );

            return new EventRSVPDto
            {
                EventId = rsvp.EventId,
                UserId = rsvp.UserId,
                RsvpStatus = rsvp.RsvpStatus.ToString()
            };
        }

        public async Task<EventRSVPDto?> GetRSVPStatusAsync(int eventId, Guid userId)
        {
            var rsvp = await _context.EventRSVPs
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (rsvp == null) return null;

            return new EventRSVPDto
            {
                EventId = rsvp.EventId,
                UserId = rsvp.UserId,
                RsvpStatus = rsvp.RsvpStatus.ToString()
            };
        }
    }
}
