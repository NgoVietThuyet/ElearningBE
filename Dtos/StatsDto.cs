using System;

namespace ElearningAPI.Dtos
{
    public class OverviewStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalNews { get; set; }
        public int TotalLessons { get; set; }
    }

    public class GpaDistributionDto
    {
        public string Range { get; set; }
        public int Count { get; set; }
    }

    public class RecentActivityDto
    {
        public string Type { get; set; } // "NEWS" or "COURSE"
        public string Title { get; set; }
        public string Action { get; set; } // e.g. "Được tạo mới"
        public string By { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
