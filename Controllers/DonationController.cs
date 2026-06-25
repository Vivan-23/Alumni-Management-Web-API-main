using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires any authenticated user
    public class DonationController : ControllerBase
    {
        [HttpGet("history")]
        public IActionResult GetDonations()
        {
            return Ok(new[]
            {
                new { Id = 101, Amount = 100.00, Date = DateTime.UtcNow.AddDays(-10), Status = "Completed" },
                new { Id = 102, Amount = 250.00, Date = DateTime.UtcNow.AddDays(-2), Status = "Completed" }
            });
        }
    }
}
