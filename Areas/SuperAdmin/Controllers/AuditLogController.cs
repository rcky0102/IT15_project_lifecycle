using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AuditLogController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /SuperAdmin/AuditLog
        public async Task<IActionResult> Index(
            string? role,
            string? actionFilter,
            string? module,
            string? search,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 25)
        {
            ViewData["Title"] = "Audit Logs";

            var query = _db.AuditLogs.AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(a => a.Role == role);

            if (!string.IsNullOrWhiteSpace(actionFilter))
                query = query.Where(a => a.Action == actionFilter);

            if (!string.IsNullOrWhiteSpace(module))
                query = query.Where(a => a.Module == module);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a =>
                    a.UserName.Contains(search) ||
                    a.Description.Contains(search) ||
                    (a.EntityType != null && a.EntityType.Contains(search)));

            if (from.HasValue)
            {
                query = query.Where(a => a.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.Timestamp <= to.Value.Date.AddDays(1));
            }

            // Totals for the filtered set
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Distinct values for filter dropdowns
            ViewData["Roles"] = await _db.AuditLogs.Select(a => a.Role).Distinct().OrderBy(r => r).ToListAsync();
            ViewData["Actions"] = await _db.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
            ViewData["Modules"] = await _db.AuditLogs.Select(a => a.Module).Distinct().OrderBy(m => m).ToListAsync();

            // Pass current filter state back to the view
            ViewData["CurrentRole"] = role;
            ViewData["CurrentAction"] = actionFilter;
            ViewData["CurrentModule"] = module;
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentFrom"] = from?.ToString("yyyy-MM-dd");
            ViewData["CurrentTo"] = to?.ToString("yyyy-MM-dd");
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = totalCount;

            return View(logs);
        }

        // GET: /SuperAdmin/AuditLog/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var log = await _db.AuditLogs.FindAsync(id);
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
                timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        // POST: /SuperAdmin/AuditLog/Export (CSV)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(
            string? role,
            string? actionFilter,
            string? module,
            string? search,
            DateTime? from,
            DateTime? to)
        {
            var query = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(a => a.Role == role);
            if (!string.IsNullOrWhiteSpace(actionFilter))
                query = query.Where(a => a.Action == actionFilter);
            if (!string.IsNullOrWhiteSpace(module))
                query = query.Where(a => a.Module == module);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a =>
                    a.UserName.Contains(search) ||
                    a.Description.Contains(search));
            if (from.HasValue)
            {
                query = query.Where(a => a.Timestamp >= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(a => a.Timestamp <= to.Value.Date.AddDays(1));
            }

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
