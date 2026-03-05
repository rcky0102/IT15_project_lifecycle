using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;

namespace project_lifecycle.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "HumanResource")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public AuditLogController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /HumanResource/AuditLog
        public async Task<IActionResult> Index(
            string? role,
            string? actionFilter,
            string? search,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 25)
        {
            ViewData["Title"] = "Audit Logs";

            var query = _db.AuditLogs
                .Where(a => a.Module == "Profile")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(a => a.Role == role);

            if (!string.IsNullOrWhiteSpace(actionFilter))
                query = query.Where(a => a.Action == actionFilter);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a =>
                    a.UserName.Contains(search) ||
                    a.Description.Contains(search) ||
                    (a.EntityType != null && a.EntityType.Contains(search)));

            if (from.HasValue)
                query = query.Where(a => a.Timestamp >= from.Value.ToUniversalTime());

            if (to.HasValue)
                query = query.Where(a => a.Timestamp <= to.Value.Date.AddDays(1).ToUniversalTime());

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var profileLogs = _db.AuditLogs.Where(a => a.Module == "Profile");
            ViewData["Roles"] = await profileLogs.Select(a => a.Role).Distinct().OrderBy(r => r).ToListAsync();
            ViewData["Actions"] = await profileLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();

            ViewData["CurrentRole"] = role;
            ViewData["CurrentAction"] = actionFilter;
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentFrom"] = from?.ToString("yyyy-MM-dd");
            ViewData["CurrentTo"] = to?.ToString("yyyy-MM-dd");
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = totalCount;

            return View(logs);
        }

        // GET: /HumanResource/AuditLog/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var log = await _db.AuditLogs.FirstOrDefaultAsync(a => a.Id == id && a.Module == "Profile");
            if (log == null) return NotFound();

            return Json(new
            {
                id = log.Id,
                userName = log.UserName,
                role = log.Role,
                action = log.Action,
                module = log.Module,
                description = log.Description,
                entityType = log.EntityType,
                entityId = log.EntityId,
                ipAddress = log.IpAddress,
                timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            });
        }

        // POST: /HumanResource/AuditLog/Export (CSV)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(
            string? role,
            string? actionFilter,
            string? search,
            DateTime? from,
            DateTime? to)
        {
            var query = _db.AuditLogs
                .Where(a => a.Module == "Profile")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(a => a.Role == role);
            if (!string.IsNullOrWhiteSpace(actionFilter))
                query = query.Where(a => a.Action == actionFilter);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a =>
                    a.UserName.Contains(search) ||
                    a.Description.Contains(search));
            if (from.HasValue)
                query = query.Where(a => a.Timestamp >= from.Value.ToUniversalTime());
            if (to.HasValue)
                query = query.Where(a => a.Timestamp <= to.Value.Date.AddDays(1).ToUniversalTime());

            var logs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,Timestamp,User,Role,Action,Module,Description,EntityType,EntityId,IpAddress");
            foreach (var l in logs)
            {
                csv.AppendLine($"\"{l.Id}\",\"{l.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{Escape(l.UserName)}\",\"{Escape(l.Role)}\",\"{Escape(l.Action)}\",\"{Escape(l.Module)}\",\"{Escape(l.Description)}\",\"{Escape(l.EntityType)}\",\"{Escape(l.EntityId)}\",\"{Escape(l.IpAddress)}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }

        private static string Escape(string? value)
            => (value ?? string.Empty).Replace("\"", "\"\"");
    }
}

