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
    public class JobService : IJobService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;

        public JobService(AppDbContext context, IAuditService auditService, IRabbitMQPublisher rabbitMQPublisher)
        {
            _context = context;
            _auditService = auditService;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public async Task<IEnumerable<JobPostingDto>> GetActiveJobsAsync()
        {
            return await _context.JobPostings
                .Where(j => j.IsActive)
                .Select(j => new JobPostingDto
                {
                    Id = j.Id,
                    UserId = j.UserId,
                    JobTitle = j.JobTitle,
                    JobDescription = j.JobDescription,
                    CompanyName = j.CompanyName,
                    Location = j.Location,
                    PostedDate = j.PostedDate,
                    ApplicationDeadline = j.ApplicationDeadline,
                    ApplyUrl = j.applyUrl,
                    IsActive = j.IsActive
                })
                .ToListAsync();
        }

        public async Task<JobPostingDto?> GetJobByIdAsync(int id)
        {
            var j = await _context.JobPostings.FindAsync(id);
            if (j == null) return null;

            return new JobPostingDto
            {
                Id = j.Id,
                UserId = j.UserId,
                JobTitle = j.JobTitle,
                JobDescription = j.JobDescription,
                CompanyName = j.CompanyName,
                Location = j.Location,
                PostedDate = j.PostedDate,
                ApplicationDeadline = j.ApplicationDeadline,
                ApplyUrl = j.applyUrl,
                IsActive = j.IsActive
            };
        }

        public async Task<JobPostingDto> CreateJobAsync(CreateJobPostingDto dto, Guid performingUserId, string? ipAddress = null)
        {
            var job = new JobPosting
            {
                UserId = performingUserId,
                JobTitle = dto.JobTitle,
                JobDescription = dto.JobDescription,
                CompanyName = dto.CompanyName,
                Location = dto.Location,
                PostedDate = DateTime.UtcNow,
                ApplicationDeadline = dto.ApplicationDeadline,
                applyUrl = dto.ApplyUrl,
                IsActive = true
            };

            _context.JobPostings.Add(job);
            await _context.SaveChangesAsync();

            // Publish job posting message to RabbitMQ exchange
            _rabbitMQPublisher.PublishJobPosted(job.Id, job.JobTitle, job.CompanyName);

            // Audit log
            await _auditService.LogAsync(
                "JobPosting.Create",
                "JobPosting",
                job.Id.ToString(),
                performingUserId,
                ipAddress,
                $"Created job posting: {job.JobTitle} at {job.CompanyName}"
            );

            return new JobPostingDto
            {
                Id = job.Id,
                UserId = job.UserId,
                JobTitle = job.JobTitle,
                JobDescription = job.JobDescription,
                CompanyName = job.CompanyName,
                Location = job.Location,
                PostedDate = job.PostedDate,
                ApplicationDeadline = job.ApplicationDeadline,
                ApplyUrl = job.applyUrl,
                IsActive = job.IsActive
            };
        }

        public async Task<JobPostingDto?> UpdateJobAsync(int id, CreateJobPostingDto dto, Guid performingUserId, string? ipAddress = null)
        {
            var job = await _context.JobPostings.FindAsync(id);
            if (job == null) return null;

            job.JobTitle = dto.JobTitle;
            job.JobDescription = dto.JobDescription;
            job.CompanyName = dto.CompanyName;
            job.Location = dto.Location;
            job.ApplicationDeadline = dto.ApplicationDeadline;
            job.applyUrl = dto.ApplyUrl;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                "JobPosting.Update",
                "JobPosting",
                job.Id.ToString(),
                performingUserId,
                ipAddress,
                $"Updated job posting: {job.JobTitle} at {job.CompanyName}"
            );

            return new JobPostingDto
            {
                Id = job.Id,
                UserId = job.UserId,
                JobTitle = job.JobTitle,
                JobDescription = job.JobDescription,
                CompanyName = job.CompanyName,
                Location = job.Location,
                PostedDate = job.PostedDate,
                ApplicationDeadline = job.ApplicationDeadline,
                ApplyUrl = job.applyUrl,
                IsActive = job.IsActive
            };
        }

        public async Task<bool> DeleteJobAsync(int id, Guid performingUserId, string? ipAddress = null)
        {
            var job = await _context.JobPostings.FindAsync(id);
            if (job == null) return false;

            // Soft delete via IsActive = false
            job.IsActive = false;
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                "JobPosting.Delete",
                "JobPosting",
                id.ToString(),
                performingUserId,
                ipAddress,
                $"Soft-deleted job posting: {job.JobTitle} (IsActive=false)"
            );

            return true;
        }
    }
}
