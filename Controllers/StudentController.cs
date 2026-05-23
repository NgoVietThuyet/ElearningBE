using ElearningAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ElearningAPI.Controllers
{
    [Authorize(Roles = "STUDENT,ADMIN,TEACHER")]
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ISseConnectionManager _sseManager;

        public StudentController(IStudentService studentService, ISseConnectionManager sseManager)
        {
            _studentService = studentService;
            _sseManager = sseManager;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("stats/overview")]
        public async Task<IActionResult> GetOverview()
        {
            return Ok(await _studentService.GetOverviewStats(GetUserId()));
        }

        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            return Ok(await _studentService.GetMyCourses(GetUserId()));
        }

        [HttpGet("courses/available")]
        public async Task<IActionResult> GetAvailableCourses()
        {
            return Ok(await _studentService.GetAvailableCourses(GetUserId()));
        }

        [HttpPost("courses/{courseId}/enroll")]
        public async Task<IActionResult> EnrollCourse(int courseId)
        {
            return Ok(await _studentService.EnrollCourse(GetUserId(), courseId));
        }

        [HttpGet("lessons")]
        public async Task<IActionResult> GetLessons()
        {
            return Ok(await _studentService.GetMyLessons(GetUserId()));
        }

        [HttpGet("lessons/{lessonId}")]
        public async Task<IActionResult> GetLessonDetail(int lessonId)
        {
            var skipEnrollment = User.IsInRole("ADMIN") || User.IsInRole("TEACHER");
            var result = await _studentService.GetLessonDetail(GetUserId(), lessonId, skipEnrollment);
            if (result == null) return NotFound(new { Message = "Lesson not found or student is not enrolled." });
            return Ok(result);
        }

        [HttpPost("lessons/{lessonId}/complete")]
        public async Task<IActionResult> CompleteLesson(int lessonId)
        {
            var userId = GetUserId();
            var result = await _studentService.CompleteLesson(userId, lessonId);
            if (!result) return NotFound(new { Message = "Lesson not found or student not enrolled." });

            // SSE: thông báo real-time tiến độ mới
            _ = Task.Run(async () =>
            {
                var progressPayload = new { studentId = userId, lessonId, completedAt = DateTime.UtcNow };
                await _sseManager.BroadcastAsync($"student-{userId}", "lesson-completed", progressPayload);
                await _sseManager.BroadcastToAdminAsync("progress-changed", new { studentId = userId, lessonId });
            });

            return Ok(new { Success = true });
        }

        [HttpPost("tests/{testId}/submit")]
        public async Task<IActionResult> SubmitTest(int testId, [FromBody] SubmitTestDto dto)
        {
            var userId = GetUserId();
            var result = await _studentService.SubmitTest(userId, testId, dto.Answers);
            if (result == null) return NotFound(new { Message = "Test not found or student is not enrolled." });

            // SSE: thông báo kết quả bài test
            _ = Task.Run(async () =>
            {
                var testPayload = new { studentId = userId, testId, completedAt = DateTime.UtcNow };
                await _sseManager.BroadcastAsync($"student-{userId}", "test-submitted", testPayload);
                await _sseManager.BroadcastToAdminAsync("progress-changed", new { studentId = userId, testId });
            });

            return Ok(result);
        }

        [HttpGet("tests/history")]
        public async Task<IActionResult> GetTestHistory()
        {
            return Ok(await _studentService.GetTestHistory(GetUserId()));
        }
    }

    public class SubmitTestDto
    {
        public List<int> Answers { get; set; } = new();
    }
}
