using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace project_lifecycle.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public Conversation? Conversation { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;
        [ForeignKey("SenderId")]
        public IdentityUser? Sender { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>Optional file attachment path.</summary>
        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        /// <summary>MIME type of attachment if any.</summary>
        [MaxLength(100)]
        public string? AttachmentType { get; set; }
    }
}
