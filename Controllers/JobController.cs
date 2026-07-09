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
    [Route("api/jobs")]
    [Authorize] // Authenticated users can view jobs
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobService;

        public JobController(IJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpGet]
        public async Task<IActionResult> GetJobs()
        {
            var jobs = await _jobService.GetActiveJobsAsync();
            return Ok(jobs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(int id)
        {
            var job = await _jobService.GetJobByIdAsync(id);
            if (job == null)
            {
                return NotFound(new { message = "Job posting not found." });
            }
            return Ok(job);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Alumni")] // Admin and Alumni
        public async Task<IActionResult> CreateJob([FromBody] CreateJobPostingDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var createdJob = await _jobService.CreateJobAsync(dto, userId, ipAddress);

            return CreatedAtAction(nameof(GetJobById), new { id = createdJob.Id }, createdJob);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Admin only
        public async Task<IActionResult> UpdateJob(int id, [FromBody] CreateJobPostingDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var updatedJob = await _jobService.UpdateJobAsync(id, dto, userId, ipAddress);

            if (updatedJob == null)
            {
                return NotFound(new { message = "Job posting not found." });
            }

            return Ok(updatedJob);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Admin only
        public async Task<IActionResult> DeleteJob(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var success = await _jobService.DeleteJobAsync(id, userId, ipAddress);

            if (!success)
            {
                return NotFound(new { message = "Job posting not found." });
            }

            return Ok(new { message = "Job posting deleted successfully (soft delete)." });
        }
    }
}
