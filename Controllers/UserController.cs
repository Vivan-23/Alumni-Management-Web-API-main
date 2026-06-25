using AlumniManagementApi.Data.AlumniManagementApi.Data;
using AlumniManagementApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Requires Admin role
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            var profiles = await _context.AlumniProfiles
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.Name);

            var result = users.Select(u => new
            {
                u.Id,
                u.Email,
                Role = u.Role.RoleName,
                RoleId = u.RoleId,
                Name = profiles.ContainsKey(u.Id) ? profiles[u.Id] : string.Empty,
                u.CreatedAt
            });

            return Ok(result);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return BadRequest(new { message = "Invalid role ID." });
            }

            user.RoleId = request.RoleId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User role updated successfully.", role = role.RoleName });
        }
    }
}
