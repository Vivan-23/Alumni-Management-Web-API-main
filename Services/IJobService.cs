using AlumniManagementApi.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface IJobService
    {
        Task<IEnumerable<JobPostingDto>> GetActiveJobsAsync();
        Task<JobPostingDto?> GetJobByIdAsync(int id);
        Task<JobPostingDto> CreateJobAsync(CreateJobPostingDto dto, Guid performingUserId, string? ipAddress = null);
        Task<JobPostingDto?> UpdateJobAsync(int id, CreateJobPostingDto dto, Guid performingUserId, string? ipAddress = null);
        Task<bool> DeleteJobAsync(int id, Guid performingUserId, string? ipAddress = null);
    }
}
