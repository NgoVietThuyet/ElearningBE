using ElearningAPI.Data;
using ElearningAPI.Models;
using ElearningAPI.Services;
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
        private readonly DocumentConversionService _conversionService;

        public PublicController(AppDbContext context, DocumentConversionService conversionService)
        {
            _context = context;
            _conversionService = conversionService;
        }

        /// <summary>GET /api/public/courses — Lấy danh sách khóa học (public)</summary>
        [HttpGet("users/{id}/avatar")]
        public async Task<IActionResult> GetUserAvatar(int id)
        {
            var avatar = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.AvatarImage,
                    u.AvatarContentType
                })
                .FirstOrDefaultAsync();

            if (avatar?.AvatarImage == null || avatar.AvatarImage.Length == 0 || string.IsNullOrWhiteSpace(avatar.AvatarContentType))
            {
                return NotFound();
            }

            return File(avatar.AvatarImage, avatar.AvatarContentType);
        }

        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.AvatarUrl,
                    c.Code,
                    c.IntroVideoUrl,
                    c.Category,
                    c.Status,
                    c.Level,
                    c.Language,
                    c.DurationMinutes,
                    c.ExpectedStudentCount,
                    c.StartDate,
                    c.EndDate,
                    c.LearningOutcomes,
                    CreatorName = c.Creator != null ? c.Creator.FullName : "Admin",
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    LessonCount = c.Lessons.Count,
                    StudentCount = c.ExpectedStudentCount > 0 ? c.ExpectedStudentCount : c.Enrollments.Count,
                    AverageProgress = c.Enrollments.Any() ? c.Enrollments.Average(e => e.ProgressPercentage) : 0,
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
                    n.AvatarUrl,
                    AuthorName = !string.IsNullOrWhiteSpace(n.AuthorName) ? n.AuthorName : n.Author != null ? n.Author.FullName : "Admin",
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
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.AvatarUrl,
                    c.Code,
                    c.IntroVideoUrl,
                    c.Category,
                    c.Status,
                    c.Level,
                    c.Language,
                    c.DurationMinutes,
                    c.ExpectedStudentCount,
                    c.StartDate,
                    c.EndDate,
                    c.LearningOutcomes,
                    CreatorName = c.Creator != null ? c.Creator.FullName : "Admin",
                    c.TeacherId,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    TeacherAvatarUrl = c.Teacher != null
                        ? (!string.IsNullOrWhiteSpace(c.Teacher.AvatarUrl)
                            ? c.Teacher.AvatarUrl
                            : (c.Teacher.AvatarImage != null ? $"/api/public/users/{c.Teacher.Id}/avatar" : null))
                        : null,
                    LessonCount = c.Lessons.Count,
                    StudentCount = c.ExpectedStudentCount > 0 ? c.ExpectedStudentCount : c.Enrollments.Count,
                    AverageProgress = c.Enrollments.Any() ? c.Enrollments.Average(e => e.ProgressPercentage) : 0,
                    c.CreatedAt,
                    Lessons = c.Lessons
                        .OrderBy(l => l.CreatedAt)
                        .Select(l => new
                        {
                            l.Id,
                            l.Title,
                            l.Description,
                            l.VideoUrl,
                            l.QuizUrl,
                            l.ArVrUrl,
                            SlideUrl = !string.IsNullOrWhiteSpace(l.SlideFileName) || !string.IsNullOrWhiteSpace(l.SlideContentType) ? $"/api/public/lessons/{l.Id}/slide" : null,
                            l.SlideFileName,
                            LessonPlanUrl = !string.IsNullOrWhiteSpace(l.LessonPlanFileName) || !string.IsNullOrWhiteSpace(l.LessonPlanContentType) ? $"/api/public/lessons/{l.Id}/lesson-plan" : null,
                            l.LessonPlanFileName,
                            PdfUrl = !string.IsNullOrWhiteSpace(l.PdfFileName) || !string.IsNullOrWhiteSpace(l.PdfContentType) ? $"/api/public/lessons/{l.Id}/pdf" : l.PdfUrl,
                            DocumentUrl = !string.IsNullOrWhiteSpace(l.DocumentFileName) || !string.IsNullOrWhiteSpace(l.DocumentContentType) ? $"/api/public/lessons/{l.Id}/document" : l.DocumentUrl,
                            DocumentName = l.DocumentFileName ?? l.DocumentName
                        }),
                    Students = c.Enrollments.Select(e => new
                    {
                        e.Student.Id,
                        e.Student.FullName,
                        e.Student.Email,
                        EnrolledAt = e.EnrolledAt
                    })
                })
                .FirstOrDefaultAsync();

            if (course == null) return NotFound();
            return Ok(course);
        }

        private IActionResult TryConvertAndServe(byte[] fileData, string? contentType, string? fileName, string? format, bool isImage = false)
        {
            if (format == "pdf" && !isImage && fileName != null)
            {
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                if (ext != ".pdf" && (ext is ".doc" or ".docx" or ".ppt" or ".pptx"))
                {
                    var pdfData = _conversionService.ConvertToPdf(fileData, fileName);
                    if (pdfData != null)
                    {
                        Response.Headers["Content-Disposition"] = $"inline";
                        return File(pdfData, "application/pdf");
                    }
                }
            }
            Response.Headers["Content-Disposition"] = $"inline";
            return File(fileData, contentType ?? "application/octet-stream");
        }

        [HttpGet("lessons/{lessonId}/pdf")]
        public async Task<IActionResult> GetLessonPdf(int lessonId, [FromQuery] bool download = false)
        {
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return NotFound();

            if (lesson.PdfFile != null && lesson.PdfFile.Length > 0)
            {
                if (download)
                    return File(lesson.PdfFile, lesson.PdfContentType ?? "application/pdf", lesson.PdfFileName ?? $"lesson-{lessonId}.pdf");
                Response.Headers["Content-Disposition"] = $"inline";
                return File(lesson.PdfFile, lesson.PdfContentType ?? "application/pdf");
            }

            if (!string.IsNullOrWhiteSpace(lesson.PdfUrl))
            {
                return Redirect(lesson.PdfUrl);
            }

            return NotFound();
        }

        [HttpGet("lessons/{lessonId}/document")]
        public async Task<IActionResult> GetLessonDocument(int lessonId, [FromQuery] bool download = false, [FromQuery] string? format = null)
        {
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return NotFound();

            if (lesson.DocumentFile != null && lesson.DocumentFile.Length > 0)
            {
                if (download)
                    return File(lesson.DocumentFile, lesson.DocumentContentType ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        lesson.DocumentFileName ?? lesson.DocumentName ?? $"lesson-{lessonId}.docx");
                return TryConvertAndServe(lesson.DocumentFile, lesson.DocumentContentType, lesson.DocumentFileName ?? lesson.DocumentName ?? "document.doc", format);
            }

            if (!string.IsNullOrWhiteSpace(lesson.DocumentUrl))
            {
                return Redirect(lesson.DocumentUrl);
            }

            return NotFound();
        }

        [HttpGet("lessons/{lessonId}/lesson-plan")]
        public async Task<IActionResult> GetLessonPlan(int lessonId, [FromQuery] bool download = false, [FromQuery] string? format = null)
        {
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson?.LessonPlanFile == null || lesson.LessonPlanFile.Length == 0) return NotFound();

            if (download)
                return File(lesson.LessonPlanFile, lesson.LessonPlanContentType ?? "application/octet-stream",
                    lesson.LessonPlanFileName ?? $"lesson-plan-{lessonId}");
            return TryConvertAndServe(lesson.LessonPlanFile, lesson.LessonPlanContentType, lesson.LessonPlanFileName ?? $"lesson-plan-{lessonId}", format);
        }

        [HttpGet("lessons/{lessonId}/slide")]
        public async Task<IActionResult> GetLessonSlide(int lessonId, [FromQuery] bool download = false, [FromQuery] string? format = null)
        {
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson?.SlideFile == null || lesson.SlideFile.Length == 0) return NotFound();

            if (download)
                return File(lesson.SlideFile, lesson.SlideContentType ?? "application/octet-stream",
                    lesson.SlideFileName ?? $"slide-{lessonId}");
            return TryConvertAndServe(lesson.SlideFile, lesson.SlideContentType, lesson.SlideFileName ?? $"slide-{lessonId}", format);
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
                news.AvatarUrl,
                AuthorName = !string.IsNullOrWhiteSpace(news.AuthorName) ? news.AuthorName : news.Author != null ? news.Author.FullName : "Admin",
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
            var totalTeachers = await _context.Users.CountAsync(u => u.Role == UserRole.TEACHER);
            var totalFeedbacks = await _context.Feedbacks.CountAsync();

            return Ok(new
            {
                totalCourses,
                totalUsers,
                totalLessons,
                totalTeachers,
                totalFeedbacks
            });
        }

        /// <summary>GET /api/public/featured-teachers — Lấy danh sách giảng viên tiêu biểu (Top 4)</summary>
        [HttpGet("featured-teachers")]
        public async Task<IActionResult> GetFeaturedTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == UserRole.TEACHER)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    // Chỉ trả về avatarUrl nếu là URL thực (http/https), bỏ qua base64 để tránh response quá lớn
                    AvatarUrl = !string.IsNullOrWhiteSpace(u.AvatarUrl)
                        ? u.AvatarUrl
                        : (u.AvatarImage != null ? $"/api/public/users/{u.Id}/avatar" : null),
                    StudentCount = u.StudentsManaged.Count,
                    LessonCount = u.CreatedLessons.Count,
                    Score = (u.StudentsManaged.Count + u.CreatedLessons.Count) / 2.0
                })
                .OrderByDescending(t => t.Score)
                .Take(4)
                .ToListAsync();

            return Ok(teachers);
        }

        /// <summary>GET /api/public/teachers — Lấy toàn bộ giảng viên để tìm kiếm public</summary>
        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == UserRole.TEACHER)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    AvatarUrl = !string.IsNullOrWhiteSpace(u.AvatarUrl)
                        ? u.AvatarUrl
                        : (u.AvatarImage != null ? $"/api/public/users/{u.Id}/avatar" : null),
                    StudentCount = u.StudentsManaged.Count,
                    LessonCount = u.CreatedLessons.Count,
                    Score = (u.StudentsManaged.Count + u.CreatedLessons.Count) / 2.0
                })
                .OrderByDescending(t => t.Score)
                .ThenBy(t => t.FullName)
                .ToListAsync();

            return Ok(teachers);
        }
    }
}
