using ElearningAPI.Data;
using ElearningAPI.Models;
using ElearningAPI.Services;
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
        private readonly ISseConnectionManager _sseManager;

        public FeedbackController(AppDbContext context, ISseConnectionManager sseManager)
        {
            _context = context;
            _sseManager = sseManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetFeedbacks(
            [FromQuery] int? courseId,
            [FromQuery] int? teacherId,
            [FromQuery] int? rating,
            [FromQuery] string? keyword,
            [FromQuery] int? limit)
        {
            var query = FeedbackQuery();

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

            var feedbacks = await query
                .OrderByDescending(f => f.CreatedAt)
                .Take(limit.HasValue && limit.Value > 0 ? limit.Value : 500)
                .ToListAsync();

            return Ok(feedbacks.Select(ToListDto));
        }

        [HttpGet("mine")]
        [Authorize(Roles = "STUDENT,TEACHER,ADMIN")]
        public async Task<IActionResult> GetMyFeedbacks()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var feedbacks = await FeedbackQuery()
                .Where(f => f.AuthorId == userId.Value || f.StudentId == userId.Value)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(feedbacks.Select(ToListDto));
        }

        [HttpGet("course/{courseId}/thread")]
        public async Task<IActionResult> GetCourseFeedbackThread(int courseId)
        {
            var feedbacks = await FeedbackQuery()
                .Where(f => f.CourseId == courseId)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();

            return Ok(BuildReplyTree(feedbacks));
        }

        [HttpGet("thread")]
        public async Task<IActionResult> GetFeedbackThread([FromQuery] int? courseId)
        {
            var query = FeedbackQuery();
            if (courseId.HasValue) query = query.Where(f => f.CourseId == courseId.Value);

            var feedbacks = await query
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();

            return Ok(BuildReplyTree(feedbacks));
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

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var feedback = new Feedback
            {
                CourseId = dto.CourseId,
                TeacherId = teacherId,
                StudentId = userId,
                AuthorId = userId,
                Rating = dto.Rating,
                Content = dto.Content.Trim(),
                Status = "Đã ghi nhận",
                CreatedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            // SSE: broadcast tới teacher và admin
            var notifyPayload = new
            {
                feedbackId = feedback.Id,
                courseId = feedback.CourseId,
                teacherId = feedback.TeacherId,
                rating = feedback.Rating,
                content = feedback.Content,
                authorId = feedback.AuthorId,
                createdAt = feedback.CreatedAt
            };
            _ = Task.Run(async () =>
            {
                await _sseManager.BroadcastAsync($"feedback-{feedback.CourseId}", "new-feedback", notifyPayload);
                await _sseManager.BroadcastAsync($"teacher-{feedback.TeacherId}", "new-feedback", notifyPayload);
                await _sseManager.BroadcastToAdminAsync("feedback-changed", new { courseId = feedback.CourseId, teacherId = feedback.TeacherId });
            });

            return CreatedAtAction(nameof(GetCourseFeedbackThread), new { courseId = feedback.CourseId }, new { feedback.Id });
        }

        [HttpPost("{feedbackId}/replies")]
        [Authorize(Roles = "STUDENT,TEACHER,ADMIN")]
        public async Task<IActionResult> CreateReply(int feedbackId, [FromBody] CreateFeedbackReplyDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var parent = await _context.Feedbacks.FirstOrDefaultAsync(f => f.Id == feedbackId);
            if (parent == null) return NotFound(new { Message = "Không tìm thấy feedback." });

            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var reply = new Feedback
            {
                CourseId = parent.CourseId,
                TeacherId = parent.TeacherId,
                StudentId = null,
                AuthorId = userId,
                ParentFeedbackId = parent.Id,
                Rating = parent.Rating,
                Content = dto.Content.Trim(),
                Status = "Đã phản hồi",
                CreatedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(reply);
            await _context.SaveChangesAsync();

            // SSE: broadcast reply mới
            var replyPayload = new
            {
                feedbackId = reply.Id,
                parentFeedbackId = reply.ParentFeedbackId,
                courseId = reply.CourseId,
                teacherId = reply.TeacherId,
                content = reply.Content,
                authorId = reply.AuthorId,
                createdAt = reply.CreatedAt
            };
            _ = Task.Run(async () =>
            {
                await _sseManager.BroadcastAsync($"feedback-{reply.CourseId}", "new-reply", replyPayload);
                await _sseManager.BroadcastAsync($"teacher-{reply.TeacherId}", "new-feedback", replyPayload);
                await _sseManager.BroadcastToAdminAsync("feedback-changed", new { courseId = reply.CourseId });
            });

            return CreatedAtAction(nameof(GetCourseFeedbackThread), new { courseId = reply.CourseId }, new { reply.Id });
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

        private static List<FeedbackThreadDto> BuildReplyTree(List<Feedback> feedbacks)
        {
            var childrenByParent = feedbacks
                .Where(f => f.ParentFeedbackId.HasValue)
                .GroupBy(f => f.ParentFeedbackId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.CreatedAt).ToList());

            FeedbackThreadDto ToTree(Feedback f)
            {
                var dto = ToThreadDto(f);
                if (childrenByParent.TryGetValue(f.Id, out var children))
                {
                    dto.Replies = children.Select(c => ToTree(c)).ToList();
                }
                return dto;
            }

            return feedbacks
                .Where(f => !f.ParentFeedbackId.HasValue)
                .OrderBy(f => f.CreatedAt)
                .Select(f => ToTree(f))
                .ToList();
        }

        private IQueryable<Feedback> FeedbackQuery()
        {
            return _context.Feedbacks
                .Include(f => f.Course)
                .Include(f => f.Teacher)
                .Include(f => f.Student)
                .Include(f => f.Author)
                .AsQueryable();
        }

        private int? GetCurrentUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdString, out var userId) ? userId : null;
        }

        private static object ToListDto(Feedback f)
        {
            var dto = ToThreadDto(f);
            return new
            {
                dto.Id,
                dto.CourseId,
                dto.CourseTitle,
                dto.TeacherId,
                dto.TeacherName,
                dto.StudentId,
                dto.StudentName,
                dto.StudentEmail,
                dto.AuthorId,
                dto.AuthorName,
                dto.AuthorRole,
                dto.ParentFeedbackId,
                dto.Rating,
                dto.Content,
                dto.Status,
                dto.CreatedAt
            };
        }

        private static FeedbackThreadDto ToThreadDto(Feedback f)
        {
            var authorName = f.Author?.FullName
                ?? f.Student?.FullName
                ?? f.Teacher?.FullName
                ?? "Người dùng";

            var authorRole = f.Author?.Role.ToString()
                ?? (f.StudentId.HasValue ? "STUDENT" : "TEACHER");

            return new FeedbackThreadDto
            {
                Id = f.Id,
                CourseId = f.CourseId,
                CourseTitle = f.Course?.Title ?? "N/A",
                TeacherId = f.TeacherId,
                TeacherName = f.Teacher?.FullName ?? "N/A",
                StudentId = f.StudentId,
                StudentName = f.Student?.FullName ?? authorName,
                StudentEmail = f.Student?.Email ?? string.Empty,
                AuthorId = f.AuthorId,
                AuthorName = authorName,
                AuthorRole = authorRole,
                ParentFeedbackId = f.ParentFeedbackId,
                Rating = f.Rating,
                Content = f.Content,
                Status = f.Status,
                CreatedAt = f.CreatedAt,
            };
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

    public class CreateFeedbackReplyDto
    {
        [Required, MinLength(2)]
        public string Content { get; set; } = string.Empty;
    }

    public class FeedbackThreadDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int? StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public int? AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public int? ParentFeedbackId { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<FeedbackThreadDto> Replies { get; set; } = new();
    }
}
