using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace project_lifecycle.Services
{
    public class SecurityLogService : ISecurityLogService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly INotificationService _notificationService;
        private static readonly ConcurrentDictionary<string, List<DateTime>> _failedLoginAttempts = new();

        public SecurityLogService(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            INotificationService notificationService)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task LogSecurityEventAsync(
            string eventType,
            string description,
            bool isSuspicious = false,
            string? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? requestPath = null,
            int threatLevel = 1,
            string? eventProperties = null)
        {
            try
            {
                var securityLog = new SecurityLog
                {
                    EventType = eventType,
                    Description = description,
                    IsSuspicious = isSuspicious,
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    RequestPath = requestPath,
                    ThreatLevel = threatLevel,
                    EventProperties = eventProperties,
                    MitigationPlan = GenerateMitigationPlan(eventType, threatLevel),
                    ContainmentStrategy = GenerateContainmentStrategy(eventType, threatLevel)
                };

                _db.SecurityLogs.Add(securityLog);
                var result = await _db.SaveChangesAsync();
                
                // Debug: Log the result
                Console.WriteLine($"SecurityLogService: Saved {result} security log entries. Event: {eventType}, Suspicious: {isSuspicious}");

                // Notify SuperAdmins if this is a suspicious event
                if (isSuspicious && threatLevel >= 3)
                {
                    await NotifySuperAdminsAsync(securityLog);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SecurityLogService ERROR: {ex.Message}");
                throw;
            }
        }

        public async Task<List<SecurityLog>> GetSecurityLogsAsync(
            string? eventType = null,
            string? search = null,
            bool? isSuspicious = null,
            int? threatLevel = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 25)
        {
            try
            {
                var query = _db.SecurityLogs.AsQueryable();

                if (!string.IsNullOrWhiteSpace(eventType))
                    query = query.Where(l => l.EventType == eventType);

                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(l =>
                        (l.UserName != null && l.UserName.Contains(search)) ||
                        l.Description.Contains(search) ||
                        (l.IpAddress != null && l.IpAddress.Contains(search)));

                if (isSuspicious.HasValue)
                    query = query.Where(l => l.IsSuspicious == isSuspicious.Value);

                if (threatLevel.HasValue)
                    query = query.Where(l => l.ThreatLevel == threatLevel.Value);

                if (from.HasValue)
                    query = query.Where(l => l.Timestamp >= from.Value);

                if (to.HasValue)
                    query = query.Where(l => l.Timestamp <= to.Value.Date.AddDays(1));

                var result = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Console.WriteLine($"SecurityLogService: Retrieved {result.Count} security logs for display");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SecurityLogService GetSecurityLogsAsync ERROR: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetSecurityLogsCountAsync(
            string? eventType = null,
            string? search = null,
            bool? isSuspicious = null,
            int? threatLevel = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = _db.SecurityLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(l => l.EventType == eventType);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l =>
                    (l.UserName != null && l.UserName.Contains(search)) ||
                    l.Description.Contains(search) ||
                    (l.IpAddress != null && l.IpAddress.Contains(search)));

            if (isSuspicious.HasValue)
                query = query.Where(l => l.IsSuspicious == isSuspicious.Value);

            if (threatLevel.HasValue)
                query = query.Where(l => l.ThreatLevel == threatLevel.Value);

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value.Date.AddDays(1));

            return await query.CountAsync();
        }

        public async Task<List<string>> GetEventTypesAsync()
        {
            return await _db.SecurityLogs
                .Select(l => l.EventType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        public async Task<ThreatReport> GenerateThreatReportAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _db.SecurityLogs.AsQueryable();

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value.Date.AddDays(1));

            var allLogs = await query.ToListAsync();

            var report = new ThreatReport
            {
                FromDate = from,
                ToDate = to,
                TotalSecurityEvents = allLogs.Count,
                SuspiciousEvents = allLogs.Count(l => l.IsSuspicious),
                CriticalThreats = allLogs.Count(l => l.ThreatLevel >= 4),
                HighThreats = allLogs.Count(l => l.ThreatLevel == 3),
                MediumThreats = allLogs.Count(l => l.ThreatLevel == 2),
                LowThreats = allLogs.Count(l => l.ThreatLevel == 1)
            };

            // Top threats by event type
            report.TopThreats = allLogs
                .Where(l => l.IsSuspicious)
                .GroupBy(l => l.EventType)
                .Select(g => new ThreatSummary
                {
                    EventType = g.Key,
                    Count = g.Count(),
                    ThreatLevel = g.Max(l => l.ThreatLevel),
                    TopIpAddress = g.GroupBy(l => l.IpAddress)
                        .OrderByDescending(x => x.Count())
                        .FirstOrDefault()?.Key ?? "Unknown",
                    TopUserName = g.GroupBy(l => l.UserName)
                        .OrderByDescending(x => x.Count())
                        .FirstOrDefault()?.Key ?? "Unknown"
                })
                .OrderByDescending(t => t.Count)
                .Take(10)
                .ToList();

            // Generate recommendations based on patterns
            report.MitigationRecommendations = GenerateMitigationRecommendations(allLogs);
            report.ContainmentStrategies = GenerateContainmentStrategies(allLogs);

            return report;
        }

        public async Task<bool> LockoutAccountAsync(string userId, string? reason = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Lock the account using Identity's built-in lockout
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTime.Now.AddYears(100));
            
            if (result.Succeeded)
            {
                // Log the lockout event
                await LogSecurityEventAsync(
                    "Account Lockout",
                    $"Account {user.Email} locked out by administrator. Reason: {reason ?? "Security precaution"}",
                    true,
                    userId,
                    user.Email,
                    null,
                    null,
                    null,
                    4,
                    JsonSerializer.Serialize(new { Reason = reason, LockedBy = "Administrator" }));

                return true;
            }

            return false;
        }

        public async Task<bool> UnlockAccountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Unlock the account
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            
            if (result.Succeeded)
            {
                // Log the unlock event
                await LogSecurityEventAsync(
                    "Account Unlock",
                    $"Account {user.Email} unlocked by administrator",
                    false,
                    userId,
                    user.Email,
                    null,
                    null,
                    null,
                    1,
                    JsonSerializer.Serialize(new { UnlockedBy = "Administrator" }));

                return true;
            }

            return false;
        }

        public async Task<bool> IsAccountLockedOutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            return lockoutEnd.HasValue && lockoutEnd.Value > DateTime.Now;
        }

        public async Task<int> GetFailedLoginAttemptsAsync(
            string? ipAddress = null,
            string? userName = null,
            TimeSpan? timeWindow = null)
        {
            var window = timeWindow ?? TimeSpan.FromMinutes(15);
            var since = DateTime.Now.Subtract(window);

            var query = _db.SecurityLogs
                .Where(l => l.EventType == "Failed Login" && l.Timestamp >= since);

            if (!string.IsNullOrWhiteSpace(ipAddress))
                query = query.Where(l => l.IpAddress == ipAddress);

            if (!string.IsNullOrWhiteSpace(userName))
                query = query.Where(l => l.UserName == userName);

            return await query.CountAsync();
        }

        private readonly ConcurrentDictionary<string, DateTime> _lockoutPeriods = new();

        public async Task<bool> LogFailedLoginAndCheckThresholdAsync(
            string userName,
            string ipAddress,
            string userAgent,
            int threshold = 5,
            TimeSpan? timeWindow = null)
        {
            var trackingKey = $"{userName}_{ipAddress}";
            var now = DateTime.Now;

            // 1. Check if user is currently in a server-side lockout period
            if (_lockoutPeriods.TryGetValue(trackingKey, out var lockoutEnd))
            {
                if (now < lockoutEnd)
                {
                    return true; // Still in lockout
                }
                else
                {
                    _lockoutPeriods.TryRemove(trackingKey, out _);
                    // Lockout expired, we can proceed
                }
            }

            var window = timeWindow ?? TimeSpan.FromMinutes(15);
            
            // 2. Clean up and add current failed attempt
            if (_failedLoginAttempts.ContainsKey(trackingKey))
            {
                _failedLoginAttempts[trackingKey] = _failedLoginAttempts[trackingKey]
                    .Where(dt => dt >= now.Subtract(window))
                    .ToList();
            }
            
            if (!_failedLoginAttempts.ContainsKey(trackingKey))
            {
                _failedLoginAttempts[trackingKey] = new List<DateTime>();
            }
            _failedLoginAttempts[trackingKey].Add(now);
            
            var currentAttempts = _failedLoginAttempts[trackingKey].Count;
            
            // 3. Check threshold
            if (currentAttempts > threshold)
            {
                // Set server-side lockout for 30 seconds
                _lockoutPeriods[trackingKey] = now.AddSeconds(30);

                // Reset attempts for this user/IP so they get a fresh start after 30s
                _failedLoginAttempts.TryRemove(trackingKey, out _);

                await LogSecurityEventAsync(
                    "Suspicious Login Activity",
                    $"Threshold exceeded: {threshold} failed login attempts detected for user '{userName}' from IP {ipAddress}. Account restricted for 30 seconds.",
                    true,
                    null,
                    userName,
                    ipAddress,
                    userAgent,
                    "/Account/Login",
                    3,
                    JsonSerializer.Serialize(new { 
                        UserName = userName, 
                        IpAddress = ipAddress, 
                        AttemptCount = currentAttempts,
                        Threshold = threshold,
                        LockoutDuration = "30s"
                    }));

                return true;
            }

            return false;
        }

        private async Task NotifySuperAdminsAsync(SecurityLog securityLog)
        {
            await _notificationService.CreateForRoleAsync(
                "SuperAdmin",
                $"Security Alert: {securityLog.EventType}",
                $"Suspicious activity detected: {securityLog.Description}. Threat Level: {securityLog.ThreatLevel}",
                "Error",
                "fas fa-shield-alt",
                $"/SuperAdmin/SecurityLog/Details/{securityLog.Id}",
                "Security"
            );
        }

        private string? GenerateMitigationPlan(string eventType, int threatLevel)
        {
            return eventType switch
            {
                "Failed Login" => threatLevel >= 3 ? "Consider implementing rate limiting or IP blocking" : "Monitor for patterns",
                "Suspicious Login Activity" => "Implement account lockout policies and require password reset",
                "Account Lockout" => "Verify user identity before unlocking account",
                "Unauthorized Access" => "Review user permissions and audit access logs",
                _ => "Review security logs and investigate further"
            };
        }

        private string? GenerateContainmentStrategy(string eventType, int threatLevel)
        {
            return eventType switch
            {
                "Failed Login" => threatLevel >= 3 ? "Block IP address temporarily" : "Monitor additional attempts",
                "Suspicious Login Activity" => "Lock user account and notify user via secure channel",
                "Account Lockout" => "Account already contained - require manual verification",
                "Unauthorized Access" => "Terminate active sessions and revoke access tokens",
                _ => "Monitor and log additional activities"
            };
        }

        private List<string> GenerateMitigationRecommendations(List<SecurityLog> logs)
        {
            var recommendations = new List<string>();

            if (logs.Count(l => l.EventType == "Failed Login") > 10)
                recommendations.Add("Implement stronger authentication policies (MFA, password complexity)");

            var suspiciousIps = logs
                .Where(l => l.IsSuspicious && l.IpAddress != null)
                .GroupBy(l => l.IpAddress)
                .Where(g => g.Count() > 3)
                .Select(g => g.Key)
                .ToList();

            if (suspiciousIps.Any())
                recommendations.Add($"Consider blocking {suspiciousIps.Count} suspicious IP addresses");

            if (logs.Count(l => l.EventType == "Suspicious Login Activity") > 5)
                recommendations.Add("Review and strengthen account lockout policies");

            return recommendations;
        }

        private List<string> GenerateContainmentStrategies(List<SecurityLog> logs)
        {
            var strategies = new List<string>();

            if (logs.Any(l => l.ThreatLevel >= 4))
                strategies.Add("Immediate account lockout for high-threat activities");

            var repeatedFailedLogins = logs
                .Where(l => l.EventType == "Failed Login")
                .GroupBy(l => new { l.IpAddress, l.UserName })
                .Where(g => g.Count() > 5)
                .ToList();

            if (repeatedFailedLogins.Any())
                strategies.Add("Implement IP-based rate limiting for repeated failures");

            if (logs.Count(l => l.EventType == "Unauthorized Access") > 0)
                strategies.Add("Conduct immediate access audit and revoke suspicious permissions");

            return strategies;
        }
    }
}
