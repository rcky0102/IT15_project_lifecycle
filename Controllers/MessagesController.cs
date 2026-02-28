using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Hubs;
using project_lifecycle.Models;

namespace project_lifecycle.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<ChatHub> _chatHub;

        public MessagesController(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            IHubContext<ChatHub> chatHub)
        {
            _db = db;
            _userManager = userManager;
            _chatHub = chatHub;
        }

        // ───────── CONTACTS (all users except SuperAdmin and self) ─────────

        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts([FromQuery] string? search)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            // Get SuperAdmin role id to exclude them
            var superAdminRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin");

            var superAdminUserIds = superAdminRole != null
                ? await _db.UserRoles
                    .Where(ur => ur.RoleId == superAdminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync()
                : new List<string>();

            var query = _db.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.UserId != currentUserId && !superAdminUserIds.Contains(e.UserId));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(e =>
                    e.FirstName.ToLower().Contains(s) ||
                    e.LastName.ToLower().Contains(s) ||
                    (e.MiddleName != null && e.MiddleName.ToLower().Contains(s)) ||
                    (e.User != null && e.User.Email != null && e.User.Email.ToLower().Contains(s)));
            }

            var employees = await query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync();

            // Get roles for each employee
            var result = new List<object>();
            foreach (var emp in employees)
            {
                var roles = emp.User != null ? await _userManager.GetRolesAsync(emp.User) : new List<string>();
                var role = roles.FirstOrDefault(r => r != "SuperAdmin") ?? "Employee";

                result.Add(new
                {
                    userId = emp.UserId,
                    employeeId = emp.Id,
                    name = $"{emp.FirstName} {emp.LastName}",
                    initials = $"{emp.FirstName[0]}{emp.LastName[0]}".ToUpper(),
                    role,
                    department = emp.Department?.Name ?? "",
                    position = emp.Position?.Name ?? "",
                    email = emp.User?.Email ?? ""
                });
            }

            return Ok(result);
        }

        // ───────── CONVERSATIONS ─────────

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var conversations = await _db.ConversationParticipants
                .Where(cp => cp.UserId == currentUserId)
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c!.Participants)
                        .ThenInclude(p => p.User)
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c!.Messages.OrderByDescending(m => m.SentAt).Take(1))
                        .ThenInclude(m => m.Sender)
                .OrderByDescending(cp => cp.Conversation!.UpdatedAt)
                .ToListAsync();

            var result = new List<object>();
            foreach (var cp in conversations)
            {
                var conv = cp.Conversation!;
                var otherParticipants = conv.Participants
                    .Where(p => p.UserId != currentUserId)
                    .ToList();

                // Get display info from Employee table
                var otherUserIds = otherParticipants.Select(p => p.UserId).ToList();
                var otherEmployees = await _db.Employees
                    .Where(e => otherUserIds.Contains(e.UserId))
                    .ToListAsync();

                var lastMessage = conv.Messages.FirstOrDefault();

                string displayName;
                string initials;
                if (conv.IsGroup)
                {
                    displayName = conv.GroupName ?? "Group Chat";
                    initials = displayName.Length >= 2 ? displayName[..2].ToUpper() : displayName.ToUpper();
                }
                else
                {
                    var otherEmp = otherEmployees.FirstOrDefault();
                    displayName = otherEmp != null
                        ? $"{otherEmp.FirstName} {otherEmp.LastName}"
                        : "Unknown";
                    initials = otherEmp != null
                        ? $"{otherEmp.FirstName[0]}{otherEmp.LastName[0]}".ToUpper()
                        : "??";
                }

                // Unread count
                var unreadCount = await _db.ChatMessages
                    .Where(m => m.ConversationId == conv.Id
                                && m.SenderId != currentUserId
                                && (cp.LastReadAt == null || m.SentAt > cp.LastReadAt))
                    .CountAsync();

                // Resolve last message sender's employee name
                string? lastMsgSenderName = null;
                if (lastMessage != null)
                {
                    if (lastMessage.SenderId == currentUserId)
                    {
                        lastMsgSenderName = "You";
                    }
                    else
                    {
                        var senderEmp = await _db.Employees
                            .FirstOrDefaultAsync(e => e.UserId == lastMessage.SenderId);
                        lastMsgSenderName = senderEmp != null
                            ? $"{senderEmp.FirstName} {senderEmp.LastName}"
                            : lastMessage.Sender?.UserName ?? "Unknown";
                    }
                }

                result.Add(new
                {
                    id = conv.Id,
                    displayName,
                    initials,
                    isGroup = conv.IsGroup,
                    lastMessage = lastMessage != null ? new
                    {
                        content = lastMessage.Content.Length > 60
                            ? lastMessage.Content[..60] + "..."
                            : lastMessage.Content,
                        senderName = lastMsgSenderName,
                        sentAt = lastMessage.SentAt,
                        timeAgo = GetTimeAgo(lastMessage.SentAt)
                    } : null,
                    unreadCount,
                    updatedAt = conv.UpdatedAt
                });
            }

            return Ok(result);
        }

        // ───────── GET OR CREATE 1-on-1 CONVERSATION ─────────

        [HttpPost("conversations/direct")]
        public async Task<IActionResult> GetOrCreateDirectConversation([FromBody] DirectConversationRequest request)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();
            if (request.OtherUserId == currentUserId) return BadRequest("Cannot message yourself.");

            // Check if 1-on-1 conversation already exists
            var existingConv = await _db.Conversations
                .Where(c => !c.IsGroup)
                .Where(c => c.Participants.Any(p => p.UserId == currentUserId)
                          && c.Participants.Any(p => p.UserId == request.OtherUserId)
                          && c.Participants.Count == 2)
                .FirstOrDefaultAsync();

            if (existingConv != null)
                return Ok(new { conversationId = existingConv.Id });

            // Create new conversation
            var conversation = new Conversation
            {
                IsGroup = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();

            _db.ConversationParticipants.AddRange(
                new ConversationParticipant { ConversationId = conversation.Id, UserId = currentUserId },
                new ConversationParticipant { ConversationId = conversation.Id, UserId = request.OtherUserId }
            );
            await _db.SaveChangesAsync();

            return Ok(new { conversationId = conversation.Id });
        }

        // ───────── MESSAGES IN A CONVERSATION ─────────

        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int before = 0, [FromQuery] int count = 50)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            // Verify user is participant
            var isParticipant = await _db.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId);
            if (!isParticipant) return Forbid();

            IQueryable<ChatMessage> query = _db.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.Sender);

            if (before > 0)
                query = query.Where(m => m.Id < before);

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .ToListAsync();

            // Get employee info for senders
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senderEmployees = await _db.Employees
                .Where(e => senderIds.Contains(e.UserId))
                .ToDictionaryAsync(e => e.UserId, e => e);

            var result = messages.OrderBy(m => m.SentAt).Select(m =>
            {
                senderEmployees.TryGetValue(m.SenderId, out var emp);
                return new
                {
                    id = m.Id,
                    conversationId = m.ConversationId,
                    senderId = m.SenderId,
                    senderName = emp != null ? $"{emp.FirstName} {emp.LastName}" : "Unknown",
                    senderInitials = emp != null ? $"{emp.FirstName[0]}{emp.LastName[0]}".ToUpper() : "??",
                    content = m.Content,
                    sentAt = m.SentAt,
                    timeAgo = GetTimeAgo(m.SentAt),
                    isOwn = m.SenderId == currentUserId,
                    attachmentUrl = m.AttachmentUrl,
                    attachmentType = m.AttachmentType
                };
            });

            return Ok(result);
        }

        // ───────── SEND MESSAGE ─────────

        [HttpPost("conversations/{conversationId}/messages")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendMessageRequest request)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            // Verify participant
            var isParticipant = await _db.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId);
            if (!isParticipant) return Forbid();

            var message = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = currentUserId,
                Content = request.Content,
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(message);

            // Update conversation timestamp
            var conv = await _db.Conversations.FindAsync(conversationId);
            if (conv != null) conv.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Get sender employee info
            var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == currentUserId);
            var senderName = emp != null ? $"{emp.FirstName} {emp.LastName}" : "Unknown";
            var senderInitials = emp != null ? $"{emp.FirstName[0]}{emp.LastName[0]}".ToUpper() : "??";

            var messageDto = new
            {
                id = message.Id,
                conversationId = message.ConversationId,
                senderId = message.SenderId,
                senderName,
                senderInitials,
                content = message.Content,
                sentAt = message.SentAt,
                timeAgo = GetTimeAgo(message.SentAt),
                attachmentUrl = message.AttachmentUrl,
                attachmentType = message.AttachmentType
            };

            // Broadcast to conversation group
            await _chatHub.Clients.Group($"conv_{conversationId}")
                .SendAsync("ReceiveMessage", messageDto);

            // Notify all participants for conversation list update
            var participantIds = await _db.ConversationParticipants
                .Where(cp => cp.ConversationId == conversationId)
                .Select(cp => cp.UserId)
                .ToListAsync();

            foreach (var pid in participantIds)
            {
                await _chatHub.Clients.Group($"user_{pid}")
                    .SendAsync("ConversationUpdated", new { conversationId });
            }

            return Ok(messageDto);
        }

        // ───────── MARK CONVERSATION AS READ ─────────

        [HttpPost("conversations/{conversationId}/read")]
        public async Task<IActionResult> MarkAsRead(int conversationId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var participant = await _db.ConversationParticipants
                .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId);

            if (participant == null) return NotFound();

            participant.LastReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok();
        }

        // ───────── TOTAL UNREAD COUNT ─────────

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetTotalUnreadCount()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var participations = await _db.ConversationParticipants
                .Where(cp => cp.UserId == currentUserId)
                .ToListAsync();

            var totalUnread = 0;
            foreach (var cp in participations)
            {
                totalUnread += await _db.ChatMessages
                    .Where(m => m.ConversationId == cp.ConversationId
                                && m.SenderId != currentUserId
                                && (cp.LastReadAt == null || m.SentAt > cp.LastReadAt))
                    .CountAsync();
            }

            return Ok(new { count = totalUnread });
        }

        // ───────── DTOs ─────────

        public class DirectConversationRequest
        {
            public string OtherUserId { get; set; } = string.Empty;
        }

        public class SendMessageRequest
        {
            public string Content { get; set; } = string.Empty;
        }

        // ───────── HELPERS ─────────

        private static string GetTimeAgo(DateTime sentAt)
        {
            var span = DateTime.UtcNow - sentAt;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d";
            return sentAt.ToString("MMM d");
        }
    }
}
