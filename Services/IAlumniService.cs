using AlumniManagementApi.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface IAlumniService
    {
        Task<IEnumerable<AlumniProfileDto>> GetAlumniAsync(int page, int pageSize, int? batchYear, string? location, string? company);
        Task<AlumniProfileDto?> GetAlumniByIdAsync(Guid id);
        Task<AlumniProfileDto?> UpdateAlumniProfileAsync(Guid id, AlumniProfileDto dto, Guid performingUserId, string? ipAddress = null);
        Task<IEnumerable<AlumniProfileDto>> SearchAlumniAsync(string query);
    }
}
