using ElearningAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElearningAPI.Controllers
{
    [Route("api/public")]
    [ApiController]
    // No [Authorize] — these are fully public endpoints
    public class PublicController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PublicController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>GET /api/public/courses — Lấy danh sách khóa học (public)</summary>
        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Creator)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    CreatorName = c.Creator != null ? c.Creator.FullName : "Admin",
                    LessonCount = c.Lessons.Count,
                    StudentCount = c.Enrollments.Count,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(courses);
        }

        /// <summary>GET /api/public/news — Lấy danh sách tin tức (public)</summary>
        [HttpGet("news")]
        public async Task<IActionResult> GetNews([FromQuery] int limit = 10)
        {
            var news = await _context.News
                .Include(n => n.Author)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Content,
                    AuthorName = n.Author != null ? n.Author.FullName : "Admin",
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(news);
        }

        /// <summary>GET /api/public/courses/{id} — Lấy chi tiết khóa học</summary>
        [HttpGet("courses/{id}")]
        public async Task<IActionResult> GetCourseById(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Creator)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            return Ok(new
            {
                course.Id,
                course.Title,
                course.Description,
                CreatorName = course.Creator != null ? course.Creator.FullName : "Admin",
                LessonCount = course.Lessons.Count,
                StudentCount = course.Enrollments.Count,
                course.CreatedAt,
                Lessons = course.Lessons.OrderBy(l => l.CreatedAt).Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.VideoUrl,
                    l.PdfUrl
                }),
                Students = course.Enrollments.Select(e => new
                {
                    e.Student.Id,
                    e.Student.FullName,
                    e.Student.Email,
                    EnrolledAt = e.EnrolledAt
                })
            });
        }

        /// <summary>GET /api/public/news/{id} — Lấy chi tiết tin tức</summary>
        [HttpGet("news/{id}")]
        public async Task<IActionResult> GetNewsById(int id)
        {
            var news = await _context.News
                .Include(n => n.Author)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null) return NotFound();

            return Ok(new
            {
                news.Id,
                news.Title,
                news.Content, // This contains HTML from TipTap
                AuthorName = news.Author != null ? news.Author.FullName : "Admin",
                news.CreatedAt
            });
        }

        /// <summary>GET /api/public/stats — Số liệu tổng quan hiển thị trên trang chủ</summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetPublicStats()
        {
            var totalCourses = await _context.Courses.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            var totalLessons = await _context.Lessons.CountAsync();

            return Ok(new
            {
                totalCourses,
                totalUsers,
                totalLessons
            });
        }
    }
}
