using System.Collections.Generic;

namespace AlumniManagementApi.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalAlumniCount { get; set; }
        public Dictionary<int, int> AlumniByBatchYear { get; set; } = new();
        public decimal TotalDonationsAmount { get; set; }
        public int ActiveJobCount { get; set; }
        public int UpcomingEventsCount { get; set; }
    }
}
