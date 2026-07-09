using AlumniManagementApi.DTOs;
using System.Threading.Tasks;

namespace AlumniManagementApi.Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
        Task<DashboardStatsDto> RecomputeAndCacheStatsAsync();
    }
}
