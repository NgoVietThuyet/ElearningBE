using ElearningAPI.Data;
using ElearningAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ElearningAPI.Controllers
{
    [Route("api/feedback")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FeedbackController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFeedbacks(
            [FromQuery] int? courseId,
            [FromQuery] int? teacherId,
            [FromQuery] int? rating,
            [FromQuery] string? keyword,
            [FromQuery] int? limit)
        {
            var query = _context.Feedbacks
                .Include(f => f.Course)
                .Include(f => f.Teacher)
                .Include(f => f.Student)
                .AsQueryable();

            if (courseId.HasValue) query = query.Where(f => f.CourseId == courseId.Value);
            if (teacherId.HasValue) query = query.Where(f => f.TeacherId == teacherId.Value);
            if (rating.HasValue) query = query.Where(f => f.Rating == rating.Value);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim().ToLower();
                query = query.Where(f =>
                    f.Course.Title.ToLower().Contains(normalizedKeyword) ||
                    f.Teacher.FullName.ToLower().Contains(normalizedKeyword) ||
                    f.Content.ToLower().Contains(normalizedKeyword));
            }

            var orderedQuery = query
                .OrderByDescending(f => f.CreatedAt)
                .Take(limit.HasValue && limit.Value > 0 ? limit.Value : 500);

            var feedbacks = await orderedQuery
                .Select(f => new
                {
                    f.Id,
                    f.CourseId,
                    CourseTitle = f.Course != null ? f.Course.Title : "N/A",
                    f.TeacherId,
                    TeacherName = f.Teacher != null ? f.Teacher.FullName : "N/A",
                    f.StudentId,
                    StudentName = f.Student != null ? f.Student.FullName : "Người dùng đã xóa",
                    StudentEmail = f.Student != null ? f.Student.Email : string.Empty,
                    f.Rating,
                    f.Content,
                    f.Status,
                    f.CreatedAt
                })
                .ToListAsync();

            return Ok(feedbacks);
        }

        [HttpGet("mine")]
        [Authorize(Roles = "STUDENT,TEACHER,ADMIN")]
        public async Task<IActionResult> GetMyFeedbacks()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out var userId)) return Unauthorized();

            var feedbacks = await _context.Feedbacks
                .Include(f => f.Course)
                .Include(f => f.Teacher)
                .Include(f => f.Student)
                .Where(f => f.StudentId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    f.Id,
                    f.CourseId,
                    CourseTitle = f.Course != null ? f.Course.Title : "N/A",
                    f.TeacherId,
                    TeacherName = f.Teacher != null ? f.Teacher.FullName : "N/A",
                    f.StudentId,
                    StudentName = f.Student != null ? f.Student.FullName : "Người dùng đã xóa",
                    StudentEmail = f.Student != null ? f.Student.Email : string.Empty,
                    f.Rating,
                    f.Content,
                    f.Status,
                    f.CreatedAt
                })
                .ToListAsync();

            return Ok(feedbacks);
        }

        [HttpPost]
        [Authorize(Roles = "STUDENT,TEACHER,ADMIN")]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var course = await _context.Courses.FindAsync(dto.CourseId);
            if (course == null) return NotFound(new { Message = "Không tìm thấy khóa học." });

            var teacherId = dto.TeacherId ?? course.CreatedBy;
            var teacher = await _context.Users.FirstOrDefaultAsync(u =>
                u.Id == teacherId && (u.Role == UserRole.TEACHER || u.Role == UserRole.ADMIN));
            if (teacher == null) return BadRequest(new { Message = "Không tìm thấy người phụ trách khóa học." });

            int? userId = null;
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out var parsedUserId)) userId = parsedUserId;

            var feedback = new Feedback
            {
                CourseId = dto.CourseId,
                TeacherId = teacherId,
                StudentId = userId,
                Rating = dto.Rating,
                Content = dto.Content.Trim(),
                Status = "Đã ghi nhận",
                CreatedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFeedbacks), new { id = feedback.Id }, new { feedback.Id });
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var courses = await _context.Courses
                .Include(c => c.Creator)
                .OrderBy(c => c.Title)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    TeacherId = c.CreatedBy,
                    TeacherName = c.Creator != null ? c.Creator.FullName : "N/A"
                })
                .ToListAsync();

            var teachers = courses
                .GroupBy(c => new { c.TeacherId, c.TeacherName })
                .Select(g => new { Id = g.Key.TeacherId, FullName = g.Key.TeacherName })
                .OrderBy(t => t.FullName)
                .ToList();

            return Ok(new { Courses = courses, Teachers = teachers });
        }
    }

    public class CreateFeedbackDto
    {
        [Required]
        public int CourseId { get; set; }

        public int? TeacherId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required, MinLength(5)]
        public string Content { get; set; } = string.Empty;
    }
}
