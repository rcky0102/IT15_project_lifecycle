using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace project_lifecycle.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The user who receives this notification.</summary>
        [Required]
        public string RecipientId { get; set; } = string.Empty;

        [ForeignKey(nameof(RecipientId))]
        public IdentityUser? Recipient { get; set; }

        /// <summary>Short title shown in the dropdown, e.g. "Proposal Approved".</summary>
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Longer description, e.g. "Your proposal 'Redesign' was approved by John."</summary>
        [Required, MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>Notification type for icon/colour: Info, Success, Warning, Error.</summary>
        [Required, MaxLength(50)]
        public string Type { get; set; } = "Info";

        /// <summary>Font-Awesome icon class, e.g. "fas fa-check-circle".</summary>
        [MaxLength(100)]
        public string? Icon { get; set; }

        /// <summary>Optional relative URL the user is taken to when clicking the notification.</summary>
        [MaxLength(500)]
        public string? Link { get; set; }

        /// <summary>Module that generated it (Proposal, Project, Task, User, System …).</summary>
        [MaxLength(100)]
        public string? Module { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }
    }
}
