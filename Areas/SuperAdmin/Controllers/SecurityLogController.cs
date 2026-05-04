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
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

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
            // If called via AJAX with JSON, parameters might be null if not using [FromBody]
            // But since we use FormData in JS, they should be populated.
            
            if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
            {
                var user = await _userManager.FindByNameAsync(userName) ?? await _userManager.FindByEmailAsync(userName);
                userId = user?.Id;
            }

            if (string.IsNullOrEmpty(userId))
            {
                var msg = "Could not identify user for lockout.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction("Index");
            }

            var result = await _securityLogService.LockoutAccountAsync(userId, reason);
            
            if (result)
            {
                var user = await _userManager.FindByIdAsync(userId);
                var msg = $"Account {user?.Email} has been locked out successfully.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = msg });
                TempData["Success"] = msg;
            }
            else
            {
                var msg = "Failed to lock out account. Please try again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
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
                var msg = "Could not identify user for unlock.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction("Index");
            }

            var result = await _securityLogService.UnlockAccountAsync(userId);
            
            if (result)
            {
                var user = await _userManager.FindByIdAsync(userId);
                var msg = $"Account {user?.Email} has been unlocked successfully.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = msg });
                TempData["Success"] = msg;
            }
            else
            {
                var msg = "Failed to unlock account. Please try again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
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
        [HttpGet]
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportToPdf(string? from, string? to)
        {
            try
            {
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                {
                    return BadRequest("Date range is required");
                }

                var fromDate = DateTime.Parse(from);
                var toDate = DateTime.Parse(to);

                var logs = await _securityLogService.GetSecurityLogsAsync(
                    null, null, null, null, 
                    fromDate, toDate);

                if (!logs.Any())
                {
                    return NotFound("No security events found in the specified date range");
                }

                // Generate PDF content using PdfSharpCore
                var pdfBytes = GeneratePdfReport(logs, from, to);
                
                return File(pdfBytes, "application/pdf", $"security-threat-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating PDF: {ex.Message}");
            }
        }

        private byte[] GeneratePdfReport(List<SecurityLog> logs, string fromDate, string toDate)
        {
            using (var ms = new MemoryStream())
            {
                var document = new PdfDocument();
                document.Info.Title = "Security Threat Intelligence Report";
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);
                
                // Professional Fonts
                var fontTitle = new XFont("Arial", 22, XFontStyle.Bold);
                var fontHeader = new XFont("Arial", 14, XFontStyle.Bold);
                var fontBody = new XFont("Arial", 10, XFontStyle.Regular);
                var fontSmall = new XFont("Arial", 8, XFontStyle.Regular);
                var fontBold = new XFont("Arial", 10, XFontStyle.Bold);

                // Professional Header Background (Navy)
                gfx.DrawRectangle(XBrushes.MidnightBlue, 0, 0, page.Width, 100);
                gfx.DrawString("SECURITY INTELLIGENCE REPORT", fontTitle, XBrushes.White, 
                    new XRect(0, 30, page.Width, 40), XStringFormats.Center);
                gfx.DrawString("PROJECT LIFECYCLE MANAGEMENT SYSTEM", fontSmall, XBrushes.LightGray,
                    new XRect(0, 65, page.Width, 20), XStringFormats.Center);

                int yPos = 130;

                // Document Information Section
                gfx.DrawString("REPORT METADATA", fontHeader, XBrushes.MidnightBlue, 40, yPos);
                yPos += 10;
                gfx.DrawLine(XPens.LightGray, 40, yPos, page.Width - 40, yPos);
                yPos += 20;

                gfx.DrawString($"Document ID:", fontBold, XBrushes.Black, 40, yPos);
                gfx.DrawString($"SEC-REP-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0,8).ToUpper()}", fontBody, XBrushes.Black, 140, yPos);
                yPos += 15;
                gfx.DrawString($"Generated At:", fontBold, XBrushes.Black, 40, yPos);
                gfx.DrawString($"{DateTime.Now:MMM dd, yyyy HH:mm:ss} UTC", fontBody, XBrushes.Black, 140, yPos);
                yPos += 15;
                gfx.DrawString($"Analysis Period:", fontBold, XBrushes.Black, 40, yPos);
                gfx.DrawString($"{fromDate} to {toDate}", fontBody, XBrushes.Black, 140, yPos);
                yPos += 40;

                // Executive Summary Section
                gfx.DrawString("EXECUTIVE SUMMARY", fontHeader, XBrushes.MidnightBlue, 40, yPos);
                yPos += 10;
                gfx.DrawLine(XPens.LightGray, 40, yPos, page.Width - 40, yPos);
                yPos += 20;

                var criticalCount = logs.Count(l => l.ThreatLevel >= 4);
                var suspiciousCount = logs.Count(l => l.IsSuspicious);
                
                gfx.DrawString($"Total Security Events Analyzed:", fontBold, XBrushes.Black, 60, yPos);
                gfx.DrawString($"{logs.Count}", fontBody, XBrushes.Black, 280, yPos);
                yPos += 18;
                gfx.DrawString($"Identified Suspicious Activities:", fontBold, XBrushes.Black, 60, yPos);
                gfx.DrawString($"{suspiciousCount}", fontBody, XBrushes.Black, 280, yPos);
                yPos += 18;
                gfx.DrawString($"Critical Threats (Level 4+):", fontBold, XBrushes.Red, 60, yPos);
                gfx.DrawString($"{criticalCount}", fontBold, XBrushes.Red, 280, yPos);
                yPos += 40;

                // Detailed Findings Table
                gfx.DrawString("DETAILED SECURITY FINDINGS", fontHeader, XBrushes.MidnightBlue, 40, yPos);
                yPos += 10;
                gfx.DrawLine(XPens.LightGray, 40, yPos, page.Width - 40, yPos);
                yPos += 20;

                // Table Header
                gfx.DrawRectangle(XBrushes.GhostWhite, 40, yPos, page.Width - 80, 25);
                gfx.DrawRectangle(XPens.LightGray, 40, yPos, page.Width - 80, 25);
                gfx.DrawString("TIMESTAMP", fontBold, XBrushes.MidnightBlue, 45, yPos + 17);
                gfx.DrawString("EVENT TYPE", fontBold, XBrushes.MidnightBlue, 150, yPos + 17);
                gfx.DrawString("RISK", fontBold, XBrushes.MidnightBlue, 300, yPos + 17);
                gfx.DrawString("ORIGIN IP", fontBold, XBrushes.MidnightBlue, 360, yPos + 17);
                gfx.DrawString("USER IDENTITY", fontBold, XBrushes.MidnightBlue, 470, yPos + 17);
                yPos += 30;

                // Table Rows (Show top incidents)
                foreach (var log in logs.OrderByDescending(l => l.ThreatLevel).ThenByDescending(l => l.Timestamp).Take(15))
                {
                    if (yPos > page.Height - 100)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        yPos = 50;
                    }

                    XBrush rowColor = log.ThreatLevel >= 4 ? XBrushes.DarkRed : (log.ThreatLevel >= 3 ? XBrushes.DarkOrange : XBrushes.Black);
                    
                    gfx.DrawString(log.Timestamp.ToString("MM/dd HH:mm"), fontSmall, XBrushes.Black, 45, yPos);
                    gfx.DrawString(log.EventType.Length > 25 ? log.EventType.Substring(0, 22) + "..." : log.EventType, fontSmall, XBrushes.Black, 150, yPos);
                    gfx.DrawString($"LEVEL {log.ThreatLevel}", fontSmall, rowColor, 300, yPos);
                    gfx.DrawString(log.IpAddress ?? "EXTERNAL", fontSmall, XBrushes.Black, 360, yPos);
                    gfx.DrawString(log.UserName ?? "ANONYMOUS", fontSmall, XBrushes.Black, 470, yPos);
                    
                    gfx.DrawLine(XPens.GhostWhite, 40, yPos + 4, page.Width - 40, yPos + 4);
                    yPos += 20;
                }

                // Mitigation & Recommendations
                yPos += 20;
                if (yPos > page.Height - 180) { page = document.AddPage(); gfx = XGraphics.FromPdfPage(page); yPos = 50; }

                gfx.DrawString("MITIGATION & STRATEGIC RECOMMENDATIONS", fontHeader, XBrushes.MidnightBlue, 40, yPos);
                yPos += 10;
                gfx.DrawLine(XPens.LightGray, 40, yPos, page.Width - 40, yPos);
                yPos += 25;

                string[] strategies = {
                    "1. IMMEDIATE REMEDIATION: Review and revoke sessions for accounts identified with high-level threats.",
                    "2. ACCESS CONTROL: Implement mandatory MFA for administrative and privileged user roles.",
                    "3. NETWORK HYGIENE: Blacklist originating IP addresses associated with persistent suspicious patterns.",
                    "4. MONITORING: Increase auditing frequency for specific endpoints identified in the table above.",
                    "5. POLICY ENFORCEMENT: Trigger automated password resets for accounts with high failed-login velocity."
                };

                foreach (var strategy in strategies)
                {
                    gfx.DrawString(strategy, fontBody, XBrushes.DarkSlateGray, 50, yPos);
                    yPos += 22;
                }

                // Footer
                var footerText = "CONFIDENTIAL - SYSTEM ADMINISTRATOR USE ONLY";
                gfx.DrawString(footerText, fontSmall, XBrushes.Gray, new XRect(0, page.Height - 40, page.Width, 20), XStringFormats.Center);
                gfx.DrawString($"Page {document.PageCount}", fontSmall, XBrushes.Gray, new XRect(0, page.Height - 40, page.Width - 40, 20), XStringFormats.BottomRight);

                document.Save(ms);
                return ms.ToArray();
            }
        }

        private static string Escape(string? value)
            => (value ?? string.Empty).Replace("\"", "\"\"");
    }
}
