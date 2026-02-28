using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace project_lifecycle.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The Identity user who performed the action.</summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public IdentityUser? User { get; set; }

        /// <summary>Display-friendly name, e.g. "John Doe".</summary>
        [Required, MaxLength(200)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Role at the time of the action (SuperAdmin, DepartmentHead, etc.).</summary>
        [Required, MaxLength(100)]
        public string Role { get; set; } = string.Empty;

        /// <summary>High-level category: Create, Update, Delete, Login, Approve, Reject, etc.</summary>
        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Area/Controller or module name, e.g. "User Management", "Projects".</summary>
        [Required, MaxLength(200)]
        public string Module { get; set; } = string.Empty;

        /// <summary>Human-readable description of what happened.</summary>
        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Optional entity type, e.g. "Project", "Employee".</summary>
        [MaxLength(200)]
        public string? EntityType { get; set; }

        /// <summary>Optional entity primary key.</summary>
        [MaxLength(200)]
        public string? EntityId { get; set; }

        /// <summary>Client IP address.</summary>
        [MaxLength(100)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
