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
    [Route("api/users")]
    [Authorize(Roles = "Admin")] // Requires Admin role for all actions
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsersAsync();
            return Ok(users);
        }

        [HttpGet("{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }
            return Ok(user);
        }

        [HttpPut("{email}/role")]
        public async Task<IActionResult> UpdateRole(string email, [FromBody] UpdateRoleRequest request)
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? adminId = Guid.TryParse(adminIdString, out var parsedAdminId) ? parsedAdminId : null;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var success = await _userService.UpdateUserRoleAsync(email, request, adminId, ipAddress);
            if (!success)
            {
                return BadRequest(new { message = "User not found or invalid role ID." });
            }

            return Ok(new { message = "User role updated successfully." });
        }

        [HttpDelete("{email}")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? adminId = Guid.TryParse(adminIdString, out var parsedAdminId) ? parsedAdminId : null;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var success = await _userService.DeleteUserAsync(email, adminId, ipAddress);
            if (!success)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new { message = "User deleted successfully." });
        }
    }
}
