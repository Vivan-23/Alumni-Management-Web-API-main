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
    [Route("api/[controller]")]
    [Authorize] // Requires any authenticated user
    public class AlumniController : ControllerBase
    {
        private readonly IAlumniService _alumniService;

        public AlumniController(IAlumniService alumniService)
        {
            _alumniService = alumniService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAlumni(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? batchYear = null,
            [FromQuery] string? location = null,
            [FromQuery] string? company = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var alumni = await _alumniService.GetAlumniAsync(page, pageSize, batchYear, location, company);
            return Ok(alumni);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlumniById(Guid id)
        {
            var profile = await _alumniService.GetAlumniByIdAsync(id);
            if (profile == null)
            {
                return NotFound(new { message = "Alumni profile not found." });
            }
            return Ok(profile);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlumniProfile(Guid id, [FromBody] AlumniProfileDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            if (id != userId)
            {
                return Forbid(); // Forbidden: can only update own profile
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var updatedProfile = await _alumniService.UpdateAlumniProfileAsync(id, dto, userId, ipAddress);

            if (updatedProfile == null)
            {
                return NotFound(new { message = "Alumni profile not found." });
            }

            return Ok(updatedProfile);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchAlumni([FromQuery] string q)
        {
            var results = await _alumniService.SearchAlumniAsync(q);
            return Ok(results);
        }
    }
}
