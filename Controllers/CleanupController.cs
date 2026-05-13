using Microsoft.AspNetCore.Mvc;
using ElearningAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace ElearningAPI.Controllers
{
    [Route("api/admin/cleanup")]
    [ApiController]
    public class CleanupController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CleanupController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunCleanup()
        {
            try
            {
                // Xóa theo thứ tự để tránh lỗi khóa ngoại (Foreign Key)
                // PHẢI XÓA CÁC BẢNG CON TRƯỚC KHI XÓA USER

                // 1. Xóa các liên kết enrollment
                var enrollments = await _context.Set<ElearningAPI.Models.Enrollment>().ToListAsync();
                _context.RemoveRange(enrollments);
                await _context.SaveChangesAsync();

                // 2. Xóa Feedbacks
                var feedbacks = await _context.Feedbacks.ToListAsync();
                _context.RemoveRange(feedbacks);
                await _context.SaveChangesAsync();

                // 3. Xóa Lessons
                var lessons = await _context.Lessons.ToListAsync();
                _context.RemoveRange(lessons);
                await _context.SaveChangesAsync();

                // 4. Xóa News
                var news = await _context.News.ToListAsync();
                _context.RemoveRange(news);
                await _context.SaveChangesAsync();

                // 5. Xóa Courses
                var courses = await _context.Courses.ToListAsync();
                _context.RemoveRange(courses);
                await _context.SaveChangesAsync();

                // 6. Xóa Users (Chỉ giữ lại thuyet@gmail.com)
                var adminEmail = "thuyet@gmail.com";
                var usersToDelete = await _context.Users.Where(u => u.Email != adminEmail).ToListAsync();
                _context.Users.RemoveRange(usersToDelete);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Dọn dẹp database thành công! Đã xóa sạch dữ liệu cũ và chỉ giữ lại tài khoản Admin." });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                return BadRequest(new { message = "Lỗi khi dọn dẹp: " + ex.Message + " | " + innerMsg });
            }
        }
    }
}
