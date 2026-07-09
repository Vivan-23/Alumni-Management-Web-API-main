using AlumniManagementApi.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface IEventService
    {
        Task<IEnumerable<EventDto>> GetEventsAsync();
        Task<EventDto?> GetEventByIdAsync(int id);
        Task<EventDto> CreateEventAsync(CreateEventDto dto, Guid performingUserId, string? ipAddress = null);
        Task<EventDto?> UpdateEventAsync(int id, CreateEventDto dto, Guid performingUserId, string? ipAddress = null);
        Task<bool> DeleteEventAsync(int id, Guid performingUserId, string? ipAddress = null);
        Task<EventRSVPDto?> CreateOrUpdateRSVPAsync(int eventId, Guid userId, string rsvpStatus, string? ipAddress = null);
        Task<EventRSVPDto?> GetRSVPStatusAsync(int eventId, Guid userId);
    }
}
