using ElearningAPI.Dtos;
using ElearningAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElearningAPI.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Invalid user token");
        }

        // --- Stats Endpoints ---
        [HttpGet("stats/overview")]
        public async Task<IActionResult> GetOverviewStats()
        {
            var stats = await _adminService.GetOverviewStatsAsync();
            return Ok(stats);
        }

        [HttpGet("stats/gpa-distribution")]
        public async Task<IActionResult> GetGpaDistribution()
        {
            var data = await _adminService.GetGpaDistributionAsync();
            return Ok(data);
        }

        [HttpGet("stats/recent-activity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            var activities = await _adminService.GetRecentActivitiesAsync(5);
            return Ok(activities);
        }

        // --- User Endpoints ---
        [HttpGet("users/get_all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("users/get_by_id/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });
            return Ok(user);
        }

        [HttpPost("users/create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _adminService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("users/update/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _adminService.UpdateUserAsync(id, dto);
            if (result == null) return NotFound(new { message = "User not found" });

            return Ok(result);
        }

        [HttpDelete("users/delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var success = await _adminService.DeleteUserAsync(id);
            if (!success) return NotFound(new { message = "User not found" });

            return Ok(new { message = "User deleted successfully" });
        }

        // --- News Endpoints ---
        [HttpGet("news/get_all")]
        public async Task<IActionResult> GetAllNews()
        {
            var news = await _adminService.GetAllNewsAsync();
            return Ok(news);
        }

        [HttpGet("news/get_by_id/{id}")]
        public async Task<IActionResult> GetNewsById(int id)
        {
            var news = await _adminService.GetNewsByIdAsync(id);
            if (news == null) return NotFound(new { message = "News not found" });
            return Ok(news);
        }

        [HttpPost("news/create")]
        public async Task<IActionResult> CreateNews([FromBody] NewsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authorId = GetCurrentUserId();
            var result = await _adminService.CreateNewsAsync(dto, authorId);
            return CreatedAtAction(nameof(GetNewsById), new { id = result.Id }, result);
        }

        [HttpPut("news/update/{id}")]
        public async Task<IActionResult> UpdateNews(int id, [FromBody] NewsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _adminService.UpdateNewsAsync(id, dto);
            if (result == null) return NotFound(new { message = "News not found" });

            return Ok(result);
        }

        [HttpDelete("news/delete/{id}")]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var success = await _adminService.DeleteNewsAsync(id);
            if (!success) return NotFound(new { message = "News not found" });

            return Ok(new { message = "News deleted successfully" });
        }

        // --- Course Endpoints ---
        [HttpGet("courses/get_all")]
        public async Task<IActionResult> GetAllCourses()
        {
            var courses = await _adminService.GetAllCoursesAsync();
            return Ok(courses);
        }

        [HttpGet("courses/get_by_id/{id}")]
        public async Task<IActionResult> GetCourseById(int id)
        {
            var course = await _adminService.GetCourseByIdAsync(id);
            if (course == null) return NotFound(new { message = "Course not found" });
            return Ok(course);
        }

        [HttpPost("courses/create")]
        public async Task<IActionResult> CreateCourse([FromBody] CourseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var adminId = GetCurrentUserId();
            var result = await _adminService.CreateCourseAsync(dto, adminId);
            return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
        }

        [HttpPut("courses/update/{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _adminService.UpdateCourseAsync(id, dto);
            if (result == null) return NotFound(new { message = "Course not found" });

            return Ok(result);
        }

        [HttpDelete("courses/delete/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var success = await _adminService.DeleteCourseAsync(id);
            if (!success) return NotFound(new { message = "Course not found" });

            return Ok(new { message = "Course deleted successfully" });
        }

        // --- Lesson Endpoints ---
        [HttpGet("lessons/get_by_course/{courseId}")]
        public async Task<IActionResult> GetLessonsByCourse(int courseId)
        {
            var lessons = await _adminService.GetLessonsByCourseAsync(courseId);
            return Ok(lessons);
        }

        [HttpGet("lessons/get_by_id/{id}")]
        public async Task<IActionResult> GetLessonById(int id)
        {
            var lesson = await _adminService.GetLessonByIdAsync(id);
            if (lesson == null) return NotFound(new { message = "Lesson not found" });
            return Ok(lesson);
        }

        [HttpPost("lessons/create")]
        public async Task<IActionResult> CreateLesson([FromBody] LessonDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var adminId = GetCurrentUserId();
            var result = await _adminService.CreateLessonAsync(dto, adminId);
            if (result == null) return NotFound(new { message = "Course not found for this lesson" });

            return CreatedAtAction(nameof(GetLessonById), new { id = result.Id }, result);
        }

        [HttpPut("lessons/update/{id}")]
        public async Task<IActionResult> UpdateLesson(int id, [FromBody] LessonDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _adminService.UpdateLessonAsync(id, dto);
            if (result == null) return NotFound(new { message = "Lesson or Course not found" });

            return Ok(result);
        }

        [HttpDelete("lessons/delete/{id}")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var success = await _adminService.DeleteLessonAsync(id);
            if (!success) return NotFound(new { message = "Lesson not found" });

            return Ok(new { message = "Lesson deleted successfully" });
        }
    }
}
