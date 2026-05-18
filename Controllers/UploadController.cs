using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ElearningAPI.Controllers
{
    [Route("api/upload")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Không có file nào được chọn." });

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (Array.IndexOf(allowedExtensions, extension) < 0)
                return BadRequest(new { message = "Định dạng file không được hỗ trợ (chỉ nhận .jpg, .png, .gif)." });

            // Kiểm tra kích thước file (Tối đa 2MB để tránh làm chậm database)
            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { message = "Kích thước file tối đa là 2MB khi lưu vào database." });

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                var fileBytes = ms.ToArray();
                
                // Chuyển đổi sang Base64 string với prefix để trình duyệt hiểu được
                string base64String = $"data:{file.ContentType};base64,{Convert.ToBase64String(fileBytes)}";
                
                return Ok(new { url = base64String });
            }
        }

        [HttpPost("news-image")]
        public async Task<IActionResult> UploadNewsImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Không có file nào được chọn." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, extension) < 0)
                return BadRequest(new { message = "Ảnh tin tức chỉ hỗ trợ JPG, PNG, GIF hoặc WEBP." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Kích thước ảnh tin tức tối đa là 5MB." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var base64String = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";

            return Ok(new { url = base64String });
        }
    }
}
