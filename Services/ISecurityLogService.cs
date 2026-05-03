using project_lifecycle.Models;

namespace project_lifecycle.Services
{
    public interface ISecurityLogService
    {
        /// <summary>Log a security event to the database</summary>
        Task LogSecurityEventAsync(
            string eventType,
            string description,
            bool isSuspicious = false,
            string? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? requestPath = null,
            int threatLevel = 1,
            string? eventProperties = null);

        /// <summary>Get security logs with filtering and pagination</summary>
        Task<List<SecurityLog>> GetSecurityLogsAsync(
            string? eventType = null,
            string? search = null,
            bool? isSuspicious = null,
            int? threatLevel = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 25);

        /// <summary>Get total count of security logs for pagination</summary>
        Task<int> GetSecurityLogsCountAsync(
            string? eventType = null,
            string? search = null,
            bool? isSuspicious = null,
            int? threatLevel = null,
            DateTime? from = null,
            DateTime? to = null);

        /// <summary>Get distinct event types for filter dropdown</summary>
        Task<List<string>> GetEventTypesAsync();

        /// <summary>Generate threat report with recommendations</summary>
        Task<ThreatReport> GenerateThreatReportAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>Lock out a user account</summary>
        Task<bool> LockoutAccountAsync(string userId, string? reason = null);

        /// <summary>Unlock a user account</summary>
        Task<bool> UnlockAccountAsync(string userId);

        /// <summary>Check if an account is locked out</summary>
        Task<bool> IsAccountLockedOutAsync(string userId);

        /// <summary>Get failed login attempts for a specific IP or username</summary>
        Task<int> GetFailedLoginAttemptsAsync(string? ipAddress = null, string? userName = null, TimeSpan? timeWindow = null);

        /// <summary>Log failed login attempt and check if threshold is exceeded</summary>
        Task<bool> LogFailedLoginAndCheckThresholdAsync(
            string userName,
            string ipAddress,
            string userAgent,
            int threshold = 5,
            TimeSpan? timeWindow = null);
    }

    public class ThreatReport
    {
        public DateTime ReportGenerated { get; set; } = DateTime.UtcNow;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TotalSecurityEvents { get; set; }
        public int SuspiciousEvents { get; set; }
        public int CriticalThreats { get; set; }
        public int HighThreats { get; set; }
        public int MediumThreats { get; set; }
        public int LowThreats { get; set; }
        public List<ThreatSummary> TopThreats { get; set; } = new();
        public List<string> MitigationRecommendations { get; set; } = new();
        public List<string> ContainmentStrategies { get; set; } = new();
    }

    public class ThreatSummary
    {
        public string EventType { get; set; } = string.Empty;
        public int Count { get; set; }
        public int ThreatLevel { get; set; }
        public string TopIpAddress { get; set; } = string.Empty;
        public string TopUserName { get; set; } = string.Empty;
    }
}
