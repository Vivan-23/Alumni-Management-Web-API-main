using System;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityType, string entityId, Guid? userId, string? ipAddress, string? details = null);
    }
}
