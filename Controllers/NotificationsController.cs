using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using project_lifecycle.Services;

namespace project_lifecycle.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<IdentityUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        /// <summary>GET api/notifications — recent notifications for the current user.</summary>
        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int count = 20)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var notifications = await _notificationService.GetRecentAsync(userId, count);
            return Ok(notifications.Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.Icon,
                n.Link,
                n.Module,
                n.IsRead,
                n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt)
            }));
        }

        /// <summary>GET api/notifications/unread-count</summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        /// <summary>POST api/notifications/{id}/read</summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok();
        }

        /// <summary>POST api/notifications/read-all</summary>
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }

        private static string GetTimeAgo(DateTime createdAt)
        {
            var span = DateTime.UtcNow - createdAt;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return createdAt.ToString("MMM d");
        }
    }
}
