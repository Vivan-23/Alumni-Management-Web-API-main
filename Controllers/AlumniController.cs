using AlumniManagementApi.Data.AlumniManagementApi.Data;
using AlumniManagementApi.DTOs;
using AlumniManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires any authenticated user
    public class AlumniController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AlumniController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAlumni()
        {
            var alumni = await _context.AlumniProfiles
                .Include(ap => ap.User)
                .Where(ap => ap.User.RoleId == 2) // Role ID 2 represents Alumni
                .ToListAsync();

            return Ok(alumni);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var profile = await _context.AlumniProfiles.FirstOrDefaultAsync(ap => ap.UserId == userId);
            if (profile == null)
            {
                return NotFound(new { message = "Alumni profile not found." });
            }

            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] AlumniProfileDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Invalid user identity." });
            }

            var profile = await _context.AlumniProfiles.FirstOrDefaultAsync(ap => ap.UserId == userId);
            if (profile == null)
            {
                // Create if not exists (Upsert)
                profile = new AlumniProfile
                {
                    UserId = userId,
                    Name = dto.Name,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    batchYear = dto.batchYear,
                    degree = dto.degree,
                    currentCompany = dto.currentCompany,
                    currentRole = dto.currentRole,
                    location = dto.location,
                    LinkedinURL = dto.LinkedinURL
                };
                _context.AlumniProfiles.Add(profile);
            }
            else
            {
                // Update fields
                profile.Name = dto.Name;
                profile.Email = dto.Email;
                profile.PhoneNumber = dto.PhoneNumber;
                profile.batchYear = dto.batchYear;
                profile.degree = dto.degree;
                profile.currentCompany = dto.currentCompany;
                profile.currentRole = dto.currentRole;
                profile.location = dto.location;
                profile.LinkedinURL = dto.LinkedinURL;
            }

            await _context.SaveChangesAsync();

            return Ok(profile);
        }
    }
}
