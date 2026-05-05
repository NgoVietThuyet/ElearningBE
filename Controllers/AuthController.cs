using ElearningAPI.Dtos;
using ElearningAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElearningAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);
            if (result == "Success") 
                return Ok(new { message = "Đăng ký thành công!" });
            
            return BadRequest(new { message = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var token = await _authService.LoginAsync(request);
            if (token == null) 
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
            
            return Ok(new { token, message = "Đăng nhập thành công!" });
        }
    }
}