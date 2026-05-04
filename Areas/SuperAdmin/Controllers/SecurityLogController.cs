using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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

            // Populate missing UserIds if possible (e.g. for logs created before UserId was tracked)
            foreach (var log in logs.Where(l => string.IsNullOrEmpty(l.UserId) && !string.IsNullOrEmpty(l.UserName)))
            {
                var user = await _userManager.FindByNameAsync(log.UserName);
                if (user != null)
                {
                    log.UserId = user.Id;
                }
                else
                {
                    // Try by email if username is an email
                    user = await _userManager.FindByEmailAsync(log.UserName);
                    if (user != null) log.UserId = user.Id;
                }
            }

            // Pre-fetch lockout status for the current page of users
            var lockoutStatus = new Dictionary<string, bool>();
            foreach (var log in logs.Where(l => !string.IsNullOrEmpty(l.UserId) || !string.IsNullOrEmpty(l.UserName)))
            {
                var targetId = !string.IsNullOrEmpty(log.UserId) ? log.UserId : log.UserName!;
                if (lockoutStatus.ContainsKey(targetId)) continue;

                var user = !string.IsNullOrEmpty(log.UserId) 
                    ? await _userManager.FindByIdAsync(log.UserId)
                    : await _userManager.FindByNameAsync(log.UserName!) ?? await _userManager.FindByEmailAsync(log.UserName!);

                if (user != null)
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    var isLocked = lockoutEnd.HasValue && lockoutEnd.Value > DateTime.Now;
                    
                    if (!string.IsNullOrEmpty(log.UserId)) lockoutStatus[log.UserId] = isLocked;
                    if (!string.IsNullOrEmpty(log.UserName)) lockoutStatus[log.UserName] = isLocked;
                }
            }
            ViewData["LockoutStatus"] = lockoutStatus;

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
        public async Task<IActionResult> LockoutAccount(string? userId, string? userName, string? reason = null)
        {
            if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
            {
                var user = await _userManager.FindByNameAsync(userName) ?? await _userManager.FindByEmailAsync(userName);
                userId = user?.Id;
            }

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Could not identify user for lockout.";
                return RedirectToAction("Index");
            }

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
        public async Task<IActionResult> UnlockAccount(string? userId, string? userName)
        {
            if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
            {
                var user = await _userManager.FindByNameAsync(userName) ?? await _userManager.FindByEmailAsync(userName);
                userId = user?.Id;
            }

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Could not identify user for unlock.";
                return RedirectToAction("Index");
            }

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

        // POST: /SuperAdmin/SecurityLog/LockoutAccount
        [HttpPost]
        public async Task<IActionResult> LockoutAccount([FromBody] dynamic data)
        {
            try
            {
                string? userId = data.userId?.ToString();
                string? userName = data.userName?.ToString();
                string? reason = data.reason?.ToString();
                
                if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
                {
                    var user = await _userManager.FindByNameAsync(userName) ?? await _userManager.FindByEmailAsync(userName);
                    userId = user?.Id;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Could not identify user for lockout." });
                }

                var result = await _securityLogService.LockoutAccountAsync(userId, reason);
                
                if (result)
                {
                    return Json(new { success = true, message = "Account locked out successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to lockout account" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: /SuperAdmin/SecurityLog/UnlockAccount
        [HttpPost]
        public async Task<IActionResult> UnlockAccount([FromBody] dynamic data)
        {
            try
            {
                string? userId = data.userId?.ToString();
                string? userName = data.userName?.ToString();
                
                if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
                {
                    var user = await _userManager.FindByNameAsync(userName) ?? await _userManager.FindByEmailAsync(userName);
                    userId = user?.Id;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Could not identify user for unlock." });
                }

                var result = await _securityLogService.UnlockAccountAsync(userId);
                
                if (result)
                {
                    return Json(new { success = true, message = "Account unlocked successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to unlock account" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
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

        // POST: /SuperAdmin/SecurityLog/ExportToPdf
        [HttpPost]
        public async Task<IActionResult> ExportToPdf([FromBody] dynamic data)
        {
            try
            {
                var fromDate = data.from?.ToString();
                var toDate = data.to?.ToString();

                if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                {
                    return Json(new { success = false, message = "Date range is required" });
                }

                var logs = await _securityLogService.GetSecurityLogsAsync(
                    null, null, null, null, 
                    DateTime.Parse(fromDate), 
                    DateTime.Parse(toDate));

                if (!logs.Any())
                {
                    return Json(new { success = false, message = "No security events found in the specified date range" });
                }

                // Generate PDF content
                var pdfBytes = GeneratePdfReport(logs, fromDate, toDate);
                
                return File(pdfBytes, "application/pdf", $"security-threat-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error generating PDF: {ex.Message}" });
            }
        }

        private byte[] GeneratePdfReport(List<SecurityLog> logs, string fromDate, string toDate)
        {
            var html = new StringBuilder();
            
            // Build HTML content
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Security Threat Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { color: #333; text-align: center; }");
            html.AppendLine("h2 { color: #666; margin-top: 30px; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<h1>Security Threat Report</h1>");
            html.AppendLine($"<h2>Report Period: {fromDate} to {toDate}</h2>");
            html.AppendLine($"<p><strong>Generated:</strong> {DateTime.Now:MMM dd, yyyy HH:mm}</p>");
            
            // Summary statistics
            var eventGroups = logs.GroupBy(l => l.EventType).ToList();
            html.AppendLine("<h2>Summary Statistics</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Event Type</th><th>Count</th><th>Percentage</th></tr>");
            
            foreach (var group in eventGroups)
            {
                var count = group.Count();
                var percentage = (count * 100.0 / logs.Count).ToString("F1");
                html.AppendLine($"<tr><td>{group.Key}</td><td>{count}</td><td>{percentage}%</td></tr>");
            }
            
            html.AppendLine("</table>");
            
            // Detailed events
            html.AppendLine("<h2>Detailed Security Events</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Timestamp</th><th>Event Type</th><th>Description</th><th>User</th><th>IP Address</th><th>Threat Level</th><th>Suspicious</th></tr>");
            
            foreach (var log in logs.OrderByDescending(l => l.Timestamp))
            {
                html.AppendLine($"<tr>");
                html.AppendLine($"<td>{log.Timestamp:MMM dd, yyyy HH:mm}</td>");
                html.AppendLine($"<td>{log.EventType}</td>");
                html.AppendLine($"<td>{log.Description}</td>");
                html.AppendLine($"<td>{log.UserName ?? "N/A"}</td>");
                html.AppendLine($"<td>{log.IpAddress ?? "N/A"}</td>");
                html.AppendLine($"<td>Level {log.ThreatLevel}</td>");
                html.AppendLine($"<td>{(log.IsSuspicious ? "Yes" : "No")}</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</table>");
            
            // Recommendations
            html.AppendLine("<h2>Mitigation & Containment Strategies</h2>");
            var suspiciousLogs = logs.Where(l => l.IsSuspicious).ToList();
            if (suspiciousLogs.Any())
            {
                html.AppendLine("<ul>");
                html.AppendLine("<li>• Monitor user accounts with repeated failed login attempts</li>");
                html.AppendLine("<li>• Consider implementing multi-factor authentication</li>");
                html.AppendLine("<li>• Review IP addresses and user agents for suspicious patterns</li>");
                html.AppendLine("</ul>");
            }
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            // Convert HTML to bytes
            var bytes = Encoding.UTF8.GetBytes(html.ToString());
            return bytes;
        }

        private static string Escape(string? value)
            => (value ?? string.Empty).Replace("\"", "\"\"");
    }
}
