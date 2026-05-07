using ElearningAPI.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElearningAPI.Services
{
    public class StudentService : IStudentService
    {
        private readonly AppDbContext _context;

        public StudentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetOverviewStats(int studentId)
        {
            var courses = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .ToListAsync();

            var avgProgress = courses.Any() ? courses.Average(e => (double)e.ProgressPercentage) : 0;

            return new
            {
                OverallProgress = (int)Math.Round(avgProgress),
                EnrolledCount = courses.Count
            };
        }

        public async Task<IEnumerable<object>> GetMyCourses(int studentId)
        {
            return await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Include(e => e.Course)
                .Select(e => new
                {
                    e.Course.Id,
                    e.Course.Title,
                    Progress = (int)Math.Round(e.ProgressPercentage),
                    TotalLessons = _context.Lessons.Count(l => l.CourseId == e.CourseId),
                    CompletedLessons = (int)Math.Round((double)e.ProgressPercentage / 100 * _context.Lessons.Count(l => l.CourseId == e.CourseId))
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetMyLessons(int studentId)
        {
            // Lấy 10 bài giảng từ các khóa học đã đăng ký
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            return await _context.Lessons
                .Where(l => enrolledCourseIds.Contains(l.CourseId))
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    Duration = "45 phút", // Giả lập thời lượng
                    Status = "current",   // Giả lập trạng thái
                    l.CourseId
                })
                .OrderByDescending(l => l.Id)
                .Take(10)
                .ToListAsync();
        }
    }
}
