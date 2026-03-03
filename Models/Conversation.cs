using System.ComponentModel.DataAnnotations;

namespace project_lifecycle.Models
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        /// <summary>True for group conversations, false for 1-on-1.</summary>
        public bool IsGroup { get; set; } = false;

        /// <summary>Optional group name (null for 1-on-1 chats).</summary>
        [MaxLength(200)]
        public string? GroupName { get; set; }

        /// <summary>UserId of the person who created this group conversation.</summary>
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
