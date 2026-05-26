using ElearningAPI.Data;
using ElearningAPI.Dtos;
using ElearningAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ElearningAPI.Controllers
{
    [Route("api/profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Token người dùng không hợp lệ");
        }

        /// <summary>GET /api/profile — Lấy thông tin cá nhân của người dùng hiện tại</summary>
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                Role = user.Role.ToString(),
                user.DateOfBirth,
                user.Gender,
                user.PhoneNumber,
                user.Address,
                user.TeachingExperienceYears,
                user.ShortBio,
                AvatarUrl = !string.IsNullOrWhiteSpace(user.AvatarUrl)
                    ? user.AvatarUrl
                    : (user.AvatarImage != null ? $"/api/public/users/{user.Id}/avatar" : null),
                user.CreatedAt,
                user.UpdatedAt
            });
        }

        /// <summary>PUT /api/profile — Cập nhật thông tin cá nhân của người dùng hiện tại</summary>
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

            user.FullName = dto.FullName;
            user.DateOfBirth = dto.DateOfBirth;
            user.Gender = dto.Gender;
            user.PhoneNumber = dto.PhoneNumber;
            user.Address = dto.Address;
            user.ShortBio = dto.ShortBio;

            if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
            {
                user.AvatarUrl = dto.AvatarUrl;
            }

            // Nếu người dùng cung cấp mật khẩu mới
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            // Xử lý upload avatar file
            if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
            {
                user.AvatarFileName = dto.AvatarFile.FileName;
                user.AvatarContentType = dto.AvatarFile.ContentType;
                using (var ms = new MemoryStream())
                {
                    await dto.AvatarFile.CopyToAsync(ms);
                    user.AvatarImage = ms.ToArray();
                }
                user.AvatarUrl = $"/api/public/users/{user.Id}/avatar";
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật thông tin cá nhân thành công!",
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    Role = user.Role.ToString(),
                    user.DateOfBirth,
                    user.Gender,
                    user.PhoneNumber,
                    user.Address,
                    user.TeachingExperienceYears,
                    user.ShortBio,
                    AvatarUrl = !string.IsNullOrWhiteSpace(user.AvatarUrl)
                        ? user.AvatarUrl
                        : (user.AvatarImage != null ? $"/api/public/users/{user.Id}/avatar" : null),
                    user.UpdatedAt
                }
            });
        }
    }
}
