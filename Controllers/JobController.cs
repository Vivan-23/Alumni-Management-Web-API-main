using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlumniManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires any authenticated user
    public class JobController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetJobs()
        {
            return Ok(new[]
            {
                new { Id = 1, Title = "Senior Dotnet Developer", Company = "Google", Location = "Bangalore" },
                new { Id = 2, Title = "Full Stack Engineer", Company = "Microsoft", Location = "Hyderabad" }
            });
        }
    }
}
