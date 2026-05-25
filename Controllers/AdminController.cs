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

        [HttpGet("stats/course-completion")]
        public async Task<IActionResult> GetCourseCompletion()
        {
            var data = await _adminService.GetCourseCompletionAsync();
            return Ok(data);
        }

        [HttpGet("stats/recent-activity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            var activities = await _adminService.GetRecentActivitiesAsync(5);
            return Ok(activities);
        }

        [HttpGet("stats/member-growth")]
        public async Task<IActionResult> GetMemberGrowth()
        {
            var data = await _adminService.GetMemberGrowthAsync();
            return Ok(data);
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
        public async Task<IActionResult> CreateUser([FromForm] CreateUserDto dto)
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
        public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            UserResponseDto? result;
            try
            {
                result = await _adminService.UpdateUserAsync(id, dto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            if (result == null) return NotFound(new { message = "User not found" });

            return Ok(result);
        }

        [HttpDelete("users/delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            bool success;
            try
            {
                success = await _adminService.DeleteUserAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

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
            try
            {
                var result = await _adminService.CreateCourseAsync(dto, adminId);
                return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("courses/update/{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _adminService.UpdateCourseAsync(id, dto);
                if (result == null) return NotFound(new { message = "Course not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("courses/delete/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var success = await _adminService.DeleteCourseAsync(id);
            if (!success) return NotFound(new { message = "Course not found" });

            return Ok(new { message = "Course deleted successfully" });
        }

        [HttpPost("courses/order")]
        public async Task<IActionResult> UpdateCourseOrder([FromBody] List<int> courseIds)
        {
            var success = await _adminService.UpdateCourseOrderAsync(courseIds);
            if (!success) return BadRequest(new { Message = "Không thể cập nhật lộ trình học tập." });
            return Ok(new { Message = "Cập nhật lộ trình học tập thành công." });
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
        public IActionResult CreateLesson([FromForm] LessonDto dto)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Quản trị viên không có quyền thêm bài giảng. Quyền này thuộc về Giảng viên phụ trách." });
        }

        [HttpPut("lessons/update/{id}")]
        public async Task<IActionResult> UpdateLesson(int id, [FromForm] LessonDto dto)
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

        [HttpGet("courses/{courseId}/materials")]
        public async Task<IActionResult> GetCourseMaterials(int courseId)
        {
            return Ok(await _adminService.GetCourseMaterialsAsync(courseId));
        }

        [HttpPost("course-materials/create")]
        public async Task<IActionResult> CreateCourseMaterial([FromBody] CourseMaterialDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _adminService.CreateCourseMaterialAsync(dto);
            if (result == null) return NotFound(new { message = "Course not found" });

            return Ok(result);
        }

        [HttpPut("course-materials/update/{id}")]
        public async Task<IActionResult> UpdateCourseMaterial(int id, [FromBody] CourseMaterialDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _adminService.UpdateCourseMaterialAsync(id, dto);
            if (result == null) return NotFound(new { message = "Material or course not found" });

            return Ok(result);
        }

        [HttpDelete("course-materials/delete/{id}")]
        public async Task<IActionResult> DeleteCourseMaterial(int id)
        {
            var success = await _adminService.DeleteCourseMaterialAsync(id);
            if (!success) return NotFound(new { message = "Material not found" });

            return Ok(new { message = "Material deleted successfully" });
        }

        [HttpGet("courses/{courseId}/learning-items")]
        public async Task<IActionResult> GetCourseLearningItems(int courseId)
        {
            return Ok(await _adminService.GetCourseLearningItemsAsync(courseId));
        }

        [HttpPost("learning-items/create")]
        public async Task<IActionResult> CreateLearningItem([FromBody] ElearningAPI.Dtos.LearningItemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _adminService.CreateLearningItemAsync(dto);
                if (result == null) return NotFound(new { message = "Lesson or course not found" });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("learning-items/update/{id}")]
        public async Task<IActionResult> UpdateLearningItem(int id, [FromBody] ElearningAPI.Dtos.LearningItemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _adminService.UpdateLearningItemAsync(id, dto);
                if (result == null) return NotFound(new { message = "Learning item, lesson, or course not found" });
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("learning-items/delete/{id}")]
        public async Task<IActionResult> DeleteLearningItem(int id)
        {
            var success = await _adminService.DeleteLearningItemAsync(id);
            if (!success) return NotFound(new { message = "Learning item not found" });

            return Ok(new { message = "Learning item deleted successfully" });
        }

        [HttpPost("courses/{courseId}/enroll")]
        public async Task<IActionResult> EnrollStudent(int courseId, [FromBody] EnrollRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _adminService.EnrollStudentInCourseAsync(courseId, dto.Email);
            if (!success) return BadRequest(new { message = "Không thể đăng ký học sinh vào khóa học. Vui lòng kiểm tra email." });
            return Ok(new { message = "Đã đăng ký học sinh vào khóa học thành công." });
        }

        [HttpDelete("courses/{courseId}/unenroll/{studentId}")]
        public async Task<IActionResult> UnenrollStudent(int courseId, int studentId)
        {
            var success = await _adminService.UnenrollStudentFromCourseAsync(courseId, studentId);
            if (!success) return NotFound(new { message = "Không tìm thấy lượt đăng ký của học sinh trong khóa học này." });
            return Ok(new { message = "Đã gỡ học sinh khỏi khóa học thành công." });
        }

        [HttpGet("enrollment-requests")]
        public async Task<IActionResult> GetEnrollmentRequests()
        {
            var requests = await _adminService.GetEnrollmentRequestsAsync();
            return Ok(requests);
        }

        [HttpPost("enrollment-requests/{requestId}/approve")]
        public async Task<IActionResult> ApproveEnrollmentRequest(int requestId)
        {
            var success = await _adminService.ApproveEnrollmentRequestAsync(requestId);
            if (!success) return BadRequest(new { message = "Không tìm thấy yêu cầu đăng ký hoặc yêu cầu đã được xử lý trước đó." });
            return Ok(new { message = "Đã phê duyệt yêu cầu đăng ký của học sinh thành công." });
        }

        [HttpPost("enrollment-requests/{requestId}/reject")]
        public async Task<IActionResult> RejectEnrollmentRequest(int requestId)
        {
            var success = await _adminService.RejectEnrollmentRequestAsync(requestId);
            if (!success) return BadRequest(new { message = "Không tìm thấy yêu cầu đăng ký hoặc yêu cầu đã được xử lý trước đó." });
            return Ok(new { message = "Đã từ chối yêu cầu đăng ký của học sinh." });
        }
    }
}
