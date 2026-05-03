using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace project_lifecycle.Models
{
    public class SecurityLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Type of security event (e.g., "Failed Login", "Suspicious Activity", "Account Lockout")</summary>
        [Required, MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>Detailed description of the security event</summary>
        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Whether this event is considered suspicious</summary>
        public bool IsSuspicious { get; set; } = false;

        /// <summary>ID of the user involved (if applicable)</summary>
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public IdentityUser? User { get; set; }

        /// <summary>Username of the user involved</summary>
        [MaxLength(256)]
        public string? UserName { get; set; }

        /// <summary>IP address from which the activity originated</summary>
        [MaxLength(100)]
        public string? IpAddress { get; set; }

        /// <summary>User agent string of the client</summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>Request path that was accessed</summary>
        [MaxLength(500)]
        public string? RequestPath { get; set; }

        /// <summary>Automated mitigation recommendations</summary>
        [MaxLength(1000)]
        public string? MitigationPlan { get; set; }

        /// <summary>Automated containment strategies</summary>
        [MaxLength(1000)]
        public string? ContainmentStrategy { get; set; }

        /// <summary>Threat level (1=Low, 2=Medium, 3=High, 4=Critical)</summary>
        public int ThreatLevel { get; set; } = 1;

        /// <summary>Whether the account was locked out due to this event</summary>
        public bool AccountLockedOut { get; set; } = false;

        /// <summary>When the account was locked out (if applicable)</summary>
        public DateTime? AccountLockoutTime { get; set; }

        /// <summary>When the event occurred</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Additional event properties stored as JSON</summary>
        [MaxLength(2000)]
        public string? EventProperties { get; set; }
    }
}
