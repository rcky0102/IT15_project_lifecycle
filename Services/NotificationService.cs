using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;

namespace project_lifecycle.Services
{
    public interface INotificationService
    {
        /// <summary>Create a notification for a specific user.</summary>
        Task CreateAsync(string recipientId, string title, string message,
            string type = "Info", string? icon = null, string? link = null, string? module = null);

        /// <summary>Create a notification for every user in a given role.</summary>
        Task CreateForRoleAsync(string roleName, string title, string message,
            string type = "Info", string? icon = null, string? link = null, string? module = null);

        /// <summary>Get the most recent notifications for a user (newest first).</summary>
        Task<List<Notification>> GetRecentAsync(string userId, int count = 20);

        /// <summary>Count of unread notifications for a user.</summary>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>Mark a single notification as read.</summary>
        Task MarkAsReadAsync(int notificationId, string userId);

        /// <summary>Mark all notifications as read for a user.</summary>
        Task MarkAllAsReadAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationService(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task CreateAsync(string recipientId, string title, string message,
            string type = "Info", string? icon = null, string? link = null, string? module = null)
        {
            var notification = new Notification
            {
                RecipientId = recipientId,
                Title = title,
                Message = message,
                Type = type,
                Icon = icon,
                Link = link,
                Module = module,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task CreateForRoleAsync(string roleName, string title, string message,
            string type = "Info", string? icon = null, string? link = null, string? module = null)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            var now = DateTime.UtcNow;

            foreach (var user in usersInRole)
            {
                _db.Notifications.Add(new Notification
                {
                    RecipientId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    Icon = icon,
                    Link = link,
                    Module = module,
                    CreatedAt = now
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetRecentAsync(string userId, int count = 20)
        {
            return await _db.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _db.Notifications
                .CountAsync(n => n.RecipientId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unread = await _db.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            await _db.SaveChangesAsync();
        }
    }
}
