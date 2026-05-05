using BCrypt.Net;
using ElearningAPI.Data;
using ElearningAPI.Models;
using ElearningAPI.Dtos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ElearningAPI.Services
{
    // Interface định nghĩa các hành động
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string?> LoginAsync(LoginDto dto);
    }

    // Service triển khai logic thực tế
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            if (dto == null)
                return "Dữ liệu đăng ký không hợp lệ.";

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return "Email và mật khẩu là bắt buộc.";

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return "Email đã tồn tại trong hệ thống.";

            string passwordHash;
            try
            {
                passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }
            catch
            {
                return "Lỗi khi xử lý mật khẩu.";
            }

            var user = new User
            {
                FullName = dto.FullName ?? string.Empty,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = UserRole.STUDENT // Mặc định là Student
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return "Success";
            }
            catch (Exception ex)
            {
                return $"Lỗi khi lưu người dùng: {ex.Message}";
            }
        }

        public async Task<string?> LoginAsync(LoginDto dto)
        {
            // Tìm người dùng theo Email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            
        // Kiểm tra người dùng và xác thực mật khẩu [cite: 268]
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;
// Nếu đúng, trả về Token xác thực [cite: 269]
            return CreateToken(user);
        }

        private string CreateToken(User user)
        {
      
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];
            var jwtDuration = _config["Jwt:DurationInMinutes"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(jwtDuration))
                throw new InvalidOperationException("JWT configuration is missing or incomplete.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtDuration)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}