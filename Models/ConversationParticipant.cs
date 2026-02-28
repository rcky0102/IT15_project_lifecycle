using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace project_lifecycle.Models
{
    public class ConversationParticipant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public Conversation? Conversation { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Timestamp of the last message the user has read in this conversation.</summary>
        public DateTime? LastReadAt { get; set; }
    }
}
