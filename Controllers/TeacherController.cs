using ElearningAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ElearningAPI.Controllers
{
    [Authorize(Roles = "TEACHER,ADMIN")]
    [Route("api/[controller]")]
    [ApiController]
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

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            return Ok(await _teacherService.GetMyStudents(GetUserId()));
        }

        [HttpGet("lessons")]
        public async Task<IActionResult> GetLessons()
        {
            return Ok(await _teacherService.GetMyLessons(GetUserId()));
        }

        [HttpGet("feedbacks")]
        public async Task<IActionResult> GetFeedbacks()
        {
            return Ok(await _teacherService.GetMyFeedbacks(GetUserId()));
        }

        [HttpPost("students/add")]
        public async Task<IActionResult> AddStudent([FromBody] string email)
        {
            var success = await _teacherService.AddStudentToClass(GetUserId(), email);
            if (!success) return BadRequest(new { Message = "Không tìm thấy học sinh hoặc email không hợp lệ." });
            return Ok(new { Message = "Đã thêm học sinh vào lớp thành công." });
        }
    }
}
