using System;

namespace ElearningAPI.Dtos
{
    public class OverviewStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalNews { get; set; }
        public int TotalLessons { get; set; }
        public CourseManagementStatsDto CourseStats { get; set; } = new();
        public UserManagementStatsDto UserStats { get; set; } = new();
    }

    public class UserManagementStatsDto
    {
        public int Total { get; set; }
        public int TotalTrend { get; set; }
        public int Active { get; set; }
        public int ActiveTrend { get; set; }
        public int Teacher { get; set; }
        public int TeacherTrend { get; set; }
        public int Student { get; set; }
        public int StudentTrend { get; set; }
    }

    public class CourseManagementStatsDto
    {
        public int Total { get; set; }
        public int TotalTrend { get; set; }
        public int Published { get; set; }
        public int PublishedTrend { get; set; }
        public int Draft { get; set; }
        public int DraftTrend { get; set; }
        public int Hidden { get; set; }
        public int HiddenTrend { get; set; }
    }

    public class GpaDistributionDto
    {
        public string Range { get; set; }
        public int Count { get; set; }
    }

    public class CourseCompletionDto
    {
        public string CourseTitle { get; set; } = string.Empty;
        public int Completed { get; set; }
        public int Incomplete { get; set; }
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
