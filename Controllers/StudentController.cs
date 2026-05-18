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

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
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

        [HttpPost("tests/{testId}/submit")]
        public async Task<IActionResult> SubmitTest(int testId, [FromBody] SubmitTestDto dto)
        {
            var result = await _studentService.SubmitTest(GetUserId(), testId, dto.Answers);
            if (result == null) return NotFound(new { Message = "Test not found or student is not enrolled." });
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
