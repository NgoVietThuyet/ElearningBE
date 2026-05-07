using ElearningAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ElearningAPI.Controllers
{
    [Authorize(Roles = "STUDENT,ADMIN")]
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

        [HttpGet("lessons")]
        public async Task<IActionResult> GetLessons()
        {
            return Ok(await _studentService.GetMyLessons(GetUserId()));
        }
    }
}
