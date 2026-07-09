using AlumniManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Requires Admin role for all actions
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetStatsAsync();
            return Ok(stats);
        }

        [AllowAnonymous]
        [HttpGet("db-check")]
        public async Task<IActionResult> DbCheck([FromServices] AlumniManagementApi.Data.AlumniManagementApi.Data.AppDbContext context)
        {
            var usersCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Users);
            var rolesCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Roles);
            var profilesCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.AlumniProfiles);
            var jobsCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.JobPostings);
            var eventsCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Events);
            var donationsCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Donations);
            
            var sampleUsers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                context.Users.Take(5).Select(u => new { u.Id, u.Email, u.RoleId })
            );
            var sampleProfiles = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                context.AlumniProfiles.Take(5).Select(p => new { p.Id, p.UserId, p.Name })
            );

            return Ok(new {
                usersCount,
                rolesCount,
                profilesCount,
                jobsCount,
                eventsCount,
                donationsCount,
                sampleUsers,
                sampleProfiles
            });
        }
    }
}
