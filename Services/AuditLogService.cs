using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using project_lifecycle.Data;
using project_lifecycle.Models;

namespace project_lifecycle.Services
{
    public interface IAuditLogService
    {
        /// <summary>
        /// Log an audit event for the current HTTP user.
        /// </summary>
        Task LogAsync(
            ClaimsPrincipal principal,
            string action,
            string module,
            string description,
            string? entityType = null,
            string? entityId = null);

        /// <summary>
        /// Log an audit event with explicit user details (for background / seed jobs).
        /// </summary>
        Task LogAsync(
            string userId,
            string userName,
            string role,
            string action,
            string module,
            string description,
            string? entityType = null,
            string? entityId = null,
            string? ipAddress = null);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            ClaimsPrincipal principal,
            string action,
            string module,
            string description,
            string? entityType = null,
            string? entityId = null)
        {
            var user = await _userManager.GetUserAsync(principal);
            if (user == null) return;

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "Unknown";

            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            await LogAsync(user.Id, user.UserName ?? user.Email ?? "Unknown", roleName, action, module, description, entityType, entityId, ip);
        }

        public async Task LogAsync(
            string userId,
            string userName,
            string role,
            string action,
            string module,
            string description,
            string? entityType = null,
            string? entityId = null,
            string? ipAddress = null)
        {
            var entry = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Role = role,
                Action = action,
                Module = module,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ipAddress ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow.AddHours(8)
            };

            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync();
        }
    }
}
