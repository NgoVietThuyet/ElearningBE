using ElearningAPI.Dtos;
using ElearningAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ElearningAPI.Controllers
{
    [Route("api/teacher")]
    [ApiController]
    [Authorize(Roles = "TEACHER,ADMIN")]
    public class TeacherController : ControllerBase
    {
        private readonly ITeacherService _teacherService;

        public TeacherController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpGet("stats/overview")]
        public async Task<IActionResult> GetOverview()
        {
            return Ok(await _teacherService.GetOverviewStats(GetUserId()));
        }

        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            return Ok(await _teacherService.GetMyCourses(GetUserId()));
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            return Ok(await _teacherService.GetMyStudents(GetUserId()));
        }

        [HttpGet("all-students")]
        public async Task<IActionResult> GetAllStudents()
        {
            return Ok(await _teacherService.GetAllStudentsAsync());
        }

        [HttpGet("students/{studentId}")]
        public async Task<IActionResult> GetStudentDetail(int studentId)
        {
            var student = await _teacherService.GetStudentDetail(GetUserId(), studentId);
            if (student == null) return NotFound(new { Message = "Khong tim thay hoc sinh trong danh sach quan ly." });
            return Ok(student);
        }

        [HttpPost("students/add")]
        public async Task<IActionResult> AddStudent([FromBody] string email)
        {
            var success = await _teacherService.AddStudentToClass(GetUserId(), email);
            if (!success) return BadRequest(new { Message = "Khong tim thay hoc sinh hoac email khong hop le." });
            return Ok(new { Message = "Da them hoc sinh vao lop thanh cong." });
        }

        [HttpPost("courses/{courseId}/enroll")]
        public async Task<IActionResult> EnrollStudent(int courseId, [FromBody] string email)
        {
            var success = await _teacherService.EnrollStudentInCourseAsync(GetUserId(), courseId, email);
            if (!success) return BadRequest(new { Message = "Khong the dang ky hoc sinh vao khoa hoc. Kiem tra email hoac quyen quan ly." });
            return Ok(new { Message = "Da dang ky hoc sinh vao khoa hoc thanh cong." });
        }

        [HttpPut("students/{studentId}")]
        public async Task<IActionResult> UpdateStudent(int studentId, [FromBody] UpdateTeacherStudentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _teacherService.UpdateStudentAsync(GetUserId(), studentId, dto.FullName);
            if (result == null) return NotFound(new { Message = "Khong tim thay hoc sinh trong danh sach quan ly." });
            return Ok(result);
        }

        [HttpDelete("students/{studentId}")]
        public async Task<IActionResult> RemoveStudent(int studentId)
        {
            var success = await _teacherService.RemoveStudentFromClass(GetUserId(), studentId);
            if (!success) return NotFound(new { Message = "Khong tim thay hoc sinh trong danh sach quan ly." });
            return Ok(new { Message = "Da go hoc sinh khoi danh sach quan ly." });
        }

        [HttpGet("lessons")]
        public async Task<IActionResult> GetLessons()
        {
            return Ok(await _teacherService.GetMyLessons(GetUserId()));
        }

        [HttpPost("lessons")]
        public async Task<IActionResult> CreateLesson([FromBody] LessonDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _teacherService.CreateLessonAsync(GetUserId(), dto);
            if (result == null) return BadRequest(new { Message = "Khoa hoc khong ton tai hoac khong thuoc quyen quan ly." });
            return Ok(result);
        }

        [HttpPut("lessons/{lessonId}")]
        public async Task<IActionResult> UpdateLesson(int lessonId, [FromBody] LessonDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _teacherService.UpdateLessonAsync(GetUserId(), lessonId, dto);
            if (result == null) return NotFound(new { Message = "Khong tim thay bai giang hoac khoa hoc khong hop le." });
            return Ok(result);
        }

        [HttpDelete("lessons/{lessonId}")]
        public async Task<IActionResult> DeleteLesson(int lessonId)
        {
            var success = await _teacherService.DeleteLessonAsync(GetUserId(), lessonId);
            if (!success) return NotFound(new { Message = "Khong tim thay bai giang." });
            return Ok(new { Message = "Da xoa bai giang." });
        }

        [HttpGet("lessons/{lessonId}/learning-items")]
        public async Task<IActionResult> GetLearningItems(int lessonId)
        {
            return Ok(await _teacherService.GetLessonLearningItems(GetUserId(), lessonId));
        }

        [HttpPost("lessons/{lessonId}/learning-items")]
        public async Task<IActionResult> CreateLearningItem(int lessonId, [FromBody] LearningItemDto dto)
        {
            var result = await _teacherService.CreateLearningItem(GetUserId(), lessonId, dto.Title, dto.Content);
            if (result == null) return NotFound(new { Message = "Khong tim thay bai giang." });
            return Ok(result);
        }

        [HttpPut("learning-items/{testId}")]
        public async Task<IActionResult> UpdateLearningItem(int testId, [FromBody] LearningItemDto dto)
        {
            var result = await _teacherService.UpdateLearningItem(GetUserId(), testId, dto.Title, dto.Content);
            if (result == null) return NotFound(new { Message = "Khong tim thay noi dung hoc tap." });
            return Ok(result);
        }

        [HttpDelete("learning-items/{testId}")]
        public async Task<IActionResult> DeleteLearningItem(int testId)
        {
            var success = await _teacherService.DeleteLearningItem(GetUserId(), testId);
            if (!success) return NotFound(new { Message = "Khong tim thay noi dung hoc tap." });
            return Ok(new { Message = "Da xoa noi dung hoc tap." });
        }

        [HttpGet("academic/progress")]
        public async Task<IActionResult> GetAcademicProgress()
        {
            return Ok(await _teacherService.GetAcademicProgress(GetUserId()));
        }

        [HttpGet("academic/test-results")]
        public async Task<IActionResult> GetTestResults()
        {
            return Ok(await _teacherService.GetTestResults(GetUserId()));
        }

        [HttpGet("reports/overview")]
        public async Task<IActionResult> GetReport()
        {
            return Ok(await _teacherService.GetReport(GetUserId()));
        }

        [HttpGet("feedbacks")]
        public async Task<IActionResult> GetFeedbacks()
        {
            return Ok(await _teacherService.GetMyFeedbacks(GetUserId()));
        }
    }

    public class UpdateTeacherStudentDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
    }

    public class LearningItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
