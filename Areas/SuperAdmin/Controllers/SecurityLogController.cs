using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SecurityLogController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISecurityLogService _securityLogService;
        private readonly UserManager<IdentityUser> _userManager;

        public SecurityLogController(
            ApplicationDbContext db,
            ISecurityLogService securityLogService,
            UserManager<IdentityUser> userManager)
        {
            _db = db;
            _securityLogService = securityLogService;
            _userManager = userManager;
        }

        // GET: /SuperAdmin/SecurityLog
        public async Task<IActionResult> Index(
            string? eventType,
            string? search,
            bool? isSuspicious,
            int? threatLevel,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 25)
        {
            ViewData["Title"] = "Security Logs";

            var logs = await _securityLogService.GetSecurityLogsAsync(
                eventType, search, isSuspicious, threatLevel, from, to, page, pageSize);

            var totalCount = await _securityLogService.GetSecurityLogsCountAsync(
                eventType, search, isSuspicious, threatLevel, from, to);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

            var eventTypes = await _securityLogService.GetEventTypesAsync();

            // Pass data to view
            ViewData["EventTypes"] = eventTypes;
            ViewData["CurrentEventType"] = eventType;
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentIsSuspicious"] = isSuspicious;
            ViewData["CurrentThreatLevel"] = threatLevel;
            ViewData["CurrentFrom"] = from?.ToString("yyyy-MM-dd");
            ViewData["CurrentTo"] = to?.ToString("yyyy-MM-dd");
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = totalCount;

            return View(logs);
        }

        // GET: /SuperAdmin/SecurityLog/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var log = await _db.SecurityLogs.FindAsync(id);
            if (log == null) return NotFound();

            return Json(new
            {
                id = log.Id,
                eventType = log.EventType,
                description = log.Description,
                isSuspicious = log.IsSuspicious,
                threatLevel = log.ThreatLevel,
                userId = log.UserId,
                userName = log.UserName,
                ipAddress = log.IpAddress,
                userAgent = log.UserAgent,
                requestPath = log.RequestPath,
                mitigationPlan = log.MitigationPlan,
                containmentStrategy = log.ContainmentStrategy,
                accountLockedOut = log.AccountLockedOut,
                accountLockoutTime = log.AccountLockoutTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                eventProperties = log.EventProperties
            });
        }

        // GET: /SuperAdmin/SecurityLog/ThreatReport
        public async Task<IActionResult> ThreatReport(DateTime? from, DateTime? to)
        {
            ViewData["Title"] = "Security Threat Report";

            var report = await _securityLogService.GenerateThreatReportAsync(from, to);

            ViewData["FromDate"] = from?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = to?.ToString("yyyy-MM-dd");

            return View(report);
        }

        // POST: /SuperAdmin/SecurityLog/LockoutAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockoutAccount(string userId, string? reason = null)
        {
            var result = await _securityLogService.LockoutAccountAsync(userId, reason);
            
            if (result)
            {
                var user = await _userManager.FindByIdAsync(userId);
                TempData["Success"] = $"Account {user?.Email} has been locked out successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to lock out account. Please try again.";
            }

            return RedirectToAction("Index");
        }

        // POST: /SuperAdmin/SecurityLog/UnlockAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(string userId)
        {
            var result = await _securityLogService.UnlockAccountAsync(userId);
            
            if (result)
            {
                var user = await _userManager.FindByIdAsync(userId);
                TempData["Success"] = $"Account {user?.Email} has been unlocked successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to unlock account. Please try again.";
            }

            return RedirectToAction("Index");
        }

        // POST: /SuperAdmin/SecurityLog/Export (CSV)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(
            string? eventType,
            string? search,
            bool? isSuspicious,
            int? threatLevel,
            DateTime? from,
            DateTime? to)
        {
            var logs = await _securityLogService.GetSecurityLogsAsync(
                eventType, search, isSuspicious, threatLevel, from, to, 1, 10000); // Get up to 10k records for export

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,Timestamp,EventType,Description,IsSuspicious,ThreatLevel,UserId,UserName,IpAddress,UserAgent,RequestPath,MitigationPlan,ContainmentStrategy,AccountLockedOut,AccountLockoutTime,EventProperties");
            
            foreach (var log in logs)
            {
                csv.AppendLine($"\"{log.Id}\"," +
                    $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                    $"\"{Escape(log.EventType)}\"," +
                    $"\"{Escape(log.Description)}\"," +
                    $"\"{log.IsSuspicious}\"," +
                    $"\"{log.ThreatLevel}\"," +
                    $"\"{Escape(log.UserId)}\"," +
                    $"\"{Escape(log.UserName)}\"," +
                    $"\"{Escape(log.IpAddress)}\"," +
                    $"\"{Escape(log.UserAgent)}\"," +
                    $"\"{Escape(log.RequestPath)}\"," +
                    $"\"{Escape(log.MitigationPlan)}\"," +
                    $"\"{Escape(log.ContainmentStrategy)}\"," +
                    $"\"{log.AccountLockedOut}\"," +
                    $"\"{log.AccountLockoutTime:yyyy-MM-dd HH:mm:ss}\"," +
                    $"\"{Escape(log.EventProperties)}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"security-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }

        // GET: /SuperAdmin/SecurityLog/GetUserLockoutStatus/{userId}
        public async Task<IActionResult> GetUserLockoutStatus(string userId)
        {
            var isLockedOut = await _securityLogService.IsAccountLockedOutAsync(userId);
            return Json(new { isLockedOut });
        }

        // GET: /SuperAdmin/SecurityLog/Test
        public async Task<IActionResult> Test()
        {
            try
            {
                // Test logging a security event
                await _securityLogService.LogSecurityEventAsync(
                    "Test Event",
                    "This is a test security event to verify logging is working",
                    false,
                    null,
                    "testuser",
                    "127.0.0.1",
                    "Test Browser",
                    "/SuperAdmin/SecurityLog/Test",
                    1);

                // Test retrieving logs
                var logs = await _securityLogService.GetSecurityLogsAsync();
                
                return Json(new { 
                    success = true, 
                    message = $"Test event logged. Total logs retrieved: {logs.Count}",
                    logCount = logs.Count,
                    lastLog = logs.FirstOrDefault()?.Description
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }

        private static string Escape(string? value)
            => (value ?? string.Empty).Replace("\"", "\"\"");
    }
}
