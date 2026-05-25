using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElearningAPI.Services;

namespace ElearningAPI.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int limit = 50)
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, limit);
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();

            var success = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { success });
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();

            var success = await _notificationService.MarkAsReadAsync(id, userId);
            if (!success) return NotFound(new { message = "Notification not found or access denied." });

            return Ok(new { success });
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("nameid")?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? "0";
            return int.TryParse(claimValue, out var id) ? id : 0;
        }
    }
}
