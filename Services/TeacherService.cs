using ElearningAPI.Data;
using ElearningAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElearningAPI.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly AppDbContext _context;

        public TeacherService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetOverviewStats(int teacherId)
        {
            var studentCount = await _context.TeacherStudents
                .CountAsync(ts => ts.TeacherId == teacherId);

            var lessonCount = await _context.Lessons
                .CountAsync(l => l.CreatedBy == teacherId);

            // Giả lập tỷ lệ hoàn thành trung bình của các học sinh trong các khóa học của giáo viên này
            var avgProgress = await _context.Enrollments
                .Where(e => _context.Courses.Any(c => c.Id == e.CourseId && c.CreatedBy == teacherId))
                .AverageAsync(e => (double?)e.ProgressPercentage) ?? 0;

            // Giả lập đánh giá trung bình (vì chưa có bảng Review/Rating, ta lấy ngẫu nhiên 4.5 - 5.0)
            var avgRating = 4.5 + (new Random().NextDouble() * 0.5);

            return new
            {
                StudentCount = studentCount,
                LessonCount = lessonCount,
                CompletionRate = $"{Math.Round(avgProgress, 1)}%",
                AvgRating = Math.Round(avgRating, 1).ToString("F1")
            };
        }

        public async Task<IEnumerable<object>> GetMyStudents(int teacherId)
        {
            return await _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Include(ts => ts.Student)
                .Select(ts => new
                {
                    ts.Student.Id,
                    ts.Student.FullName,
                    ts.Student.Email,
                    // Lấy tiến độ trung bình của học sinh này trên tất cả các khóa học
                    Progress = Math.Round(_context.Enrollments
                        .Where(e => e.StudentId == ts.StudentId)
                        .Average(e => (double?)e.ProgressPercentage) ?? 0, 1),
                    Status = "Đang học",
                    ts.CreatedAt
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetMyLessons(int teacherId)
        {
            return await _context.Lessons
                .Where(l => l.CreatedBy == teacherId)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    StudentCount = _context.Enrollments.Count(e => e.CourseId == l.CourseId),
                    // Tiến độ trung bình của bài giảng này (giả lập dựa trên Enrollment của Course)
                    Progress = Math.Round(_context.Enrollments
                        .Where(e => e.CourseId == l.CourseId)
                        .Average(e => (double?)e.ProgressPercentage) ?? 0, 1),
                    Date = l.CreatedAt.ToString("yyyy-MM-dd")
                })
                .OrderByDescending(l => l.Id)
                .Take(10)
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetMyFeedbacks(int teacherId)
        {
            // Vì chưa có bảng Feedback, ta tạo dữ liệu giả lập từ danh sách học sinh
            var students = await _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Include(ts => ts.Student)
                .Take(5)
                .ToListAsync();

            return students.Select(ts => new
            {
                Id = ts.StudentId,
                Student = ts.Student.FullName,
                Course = "Khóa học của tôi",
                Content = "Nội dung bài giảng rất dễ hiểu và trực quan. Em cảm ơn thầy/cô!",
                Date = DateTime.UtcNow.AddDays(-new Random().Next(1, 10)).ToString("yyyy-MM-dd")
            });
        }

        public async Task<bool> AddStudentToClass(int teacherId, string studentEmail)
        {
            var student = await _context.Users.FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == UserRole.STUDENT);
            if (student == null) return false;

            var exists = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == student.Id);
            if (exists) return true;

            var ts = new TeacherStudent
            {
                TeacherId = teacherId,
                StudentId = student.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.TeacherStudents.Add(ts);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
