using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Requires Admin role
    public class DashboardController : ControllerBase
    {
        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            return Ok(new
            {
                TotalAlumni = 450,
                TotalJobsPosted = 89,
                ActiveEvents = 5,
                TotalDonationsReceived = 15400.50m
            });
        }
    }
}
