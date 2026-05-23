using ElearningAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ElearningAPI.Controllers
{
    [Route("api/sse")]
    [ApiController]
    public class SseController : ControllerBase
    {
        private readonly ISseConnectionManager _sseManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SseController> _logger;

        public SseController(ISseConnectionManager sseManager, IConfiguration configuration, ILogger<SseController> logger)
        {
            _sseManager = sseManager;
            _configuration = configuration;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/sse/admin?token=...
        // Admin kết nối để nhận updates về stats (feedback mới, tiến độ học sinh,...)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("admin")]
        public async Task AdminStream([FromQuery] string token)
        {
            var claims = ValidateToken(token);
            if (claims == null || !claims.IsInRole("ADMIN"))
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await Response.WriteAsync("Unauthorized");
                return;
            }

            await StreamSse(Response, "admin-stats", HttpContext.RequestAborted);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/sse/teacher?token=...
        // Teacher kết nối để nhận updates (lesson đổi, học sinh đổi, feedback,...)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("teacher")]
        public async Task TeacherStream([FromQuery] string token)
        {
            var claims = ValidateToken(token);
            if (claims == null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await Response.WriteAsync("Unauthorized");
                return;
            }

            var role = claims.FindFirst(ClaimTypes.Role)?.Value ?? claims.FindFirst("role")?.Value ?? "";
            if (role != "TEACHER" && role != "ADMIN")
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                await Response.WriteAsync("Forbidden");
                return;
            }

            var userId = GetUserIdFromClaims(claims);
            var channel = $"teacher-{userId}";

            await StreamSse(Response, channel, HttpContext.RequestAborted);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/sse/feedback/{courseId}?token=...
        // Lắng nghe feedback real-time cho 1 khóa học cụ thể
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("feedback/{courseId:int}")]
        public async Task FeedbackStream(int courseId, [FromQuery] string token)
        {
            var claims = ValidateToken(token);
            if (claims == null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await Response.WriteAsync("Unauthorized");
                return;
            }

            var channel = $"feedback-{courseId}";
            await StreamSse(Response, channel, HttpContext.RequestAborted);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/sse/student?token=...
        // Student lắng nghe cập nhật tiến độ của chính mình (multi-tab sync)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("student")]
        public async Task StudentStream([FromQuery] string token)
        {
            var claims = ValidateToken(token);
            if (claims == null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await Response.WriteAsync("Unauthorized");
                return;
            }

            var userId = GetUserIdFromClaims(claims);
            var channel = $"student-{userId}";

            await StreamSse(Response, channel, HttpContext.RequestAborted);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/sse/status
        // Health check + số connections đang mở
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("status")]
        [AllowAnonymous]
        public IActionResult Status()
        {
            return Ok(new
            {
                Status = "SSE service is running",
                Timestamp = DateTime.UtcNow
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private async Task StreamSse(HttpResponse response, string channel, CancellationToken cancellationToken)
        {
            // SSE required headers
            response.Headers["Content-Type"] = "text/event-stream";
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["Connection"] = "keep-alive";
            response.Headers["X-Accel-Buffering"] = "no"; // Disable Nginx buffering

            // Send initial connection confirmation
            await response.WriteAsync($"event: connected\ndata: {{\"channel\":\"{channel}\",\"timestamp\":\"{DateTime.UtcNow:O}\"}}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);

            // Start heartbeat (every 25 seconds to prevent timeout)
            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = HeartbeatLoopAsync(response, heartbeatCts.Token);

            // Register client and hold connection open
            await _sseManager.AddClientAsync(channel, response, cancellationToken);
        }

        private static async Task HeartbeatLoopAsync(HttpResponse response, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(25), cancellationToken);

                    if (cancellationToken.IsCancellationRequested) break;

                    // SSE comment line as heartbeat (": ping" is ignored by EventSource)
                    await response.WriteAsync($": ping {DateTime.UtcNow:O}\n\n", cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal — client disconnected
            }
            catch (IOException)
            {
                // Client disconnected mid-write
            }
        }

        private ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                var key = _configuration["Jwt:Key"]!;
                var issuer = _configuration["Jwt:Issuer"]!;
                var audience = _configuration["Jwt:Audience"]!;

                var handler = new JwtSecurityTokenHandler();
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                };

                return handler.ValidateToken(token, validationParams, out _);
            }
            catch
            {
                return null;
            }
        }

        private static int GetUserIdFromClaims(ClaimsPrincipal claims)
        {
            var value = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? claims.FindFirst("nameid")?.Value
                     ?? claims.FindFirst("sub")?.Value
                     ?? "0";
            return int.TryParse(value, out var id) ? id : 0;
        }
    }
}
