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

            var s = search?.Trim().ToLower() ?? "";

            // Build a unified list of contacts from ALL role tables
            var contacts = new List<(string UserId, string FirstName, string? MiddleName, string LastName, string? DepartmentName, string? PositionName, string? Email)>();

            // ── Employees ──
            var empQuery = _db.Employees.Include(e => e.User).Include(e => e.Department).Include(e => e.Position)
                .Where(e => e.UserId != currentUserId && !superAdminUserIds.Contains(e.UserId));
            if (!string.IsNullOrEmpty(s))
                empQuery = empQuery.Where(e => e.FirstName.ToLower().Contains(s) || e.LastName.ToLower().Contains(s) || (e.MiddleName != null && e.MiddleName.ToLower().Contains(s)) || (e.User != null && e.User.Email != null && e.User.Email.ToLower().Contains(s)));
            var emps = await empQuery.ToListAsync();
            contacts.AddRange(emps.Select(e => (e.UserId, e.FirstName, e.MiddleName, e.LastName, e.Department?.Name, e.Position?.Name, e.User?.Email)));

            // ── Project Managers ──
            var pmQuery = _db.ProjectManagers.Include(p => p.Department).Include(p => p.Position)
                .Where(p => p.UserId != currentUserId && !superAdminUserIds.Contains(p.UserId));
            if (!string.IsNullOrEmpty(s))
                pmQuery = pmQuery.Where(p => p.FirstName.ToLower().Contains(s) || p.LastName.ToLower().Contains(s) || (p.MiddleName != null && p.MiddleName.ToLower().Contains(s)));
            var pms = await pmQuery.ToListAsync();
            contacts.AddRange(pms.Select(p => (p.UserId, p.FirstName, p.MiddleName, p.LastName, p.Department?.Name, p.Position?.Name, (string?)null)));

            // ── Department Heads ──
            var dhQuery = _db.DepartmentHeads.Include(d => d.Department).Include(d => d.Position)
                .Where(d => d.UserId != currentUserId && !superAdminUserIds.Contains(d.UserId));
            if (!string.IsNullOrEmpty(s))
                dhQuery = dhQuery.Where(d => d.FirstName.ToLower().Contains(s) || d.LastName.ToLower().Contains(s) || (d.MiddleName != null && d.MiddleName.ToLower().Contains(s)));
            var dhs = await dhQuery.ToListAsync();
            contacts.AddRange(dhs.Select(d => (d.UserId, d.FirstName, d.MiddleName, d.LastName, d.Department?.Name, d.Position?.Name, (string?)null)));

            // ── Human Resources ──
            var hrQuery = _db.HumanResources.Include(h => h.Department).Include(h => h.Position)
                .Where(h => h.UserId != currentUserId && !superAdminUserIds.Contains(h.UserId));
            if (!string.IsNullOrEmpty(s))
                hrQuery = hrQuery.Where(h => h.FirstName.ToLower().Contains(s) || h.LastName.ToLower().Contains(s) || (h.MiddleName != null && h.MiddleName.ToLower().Contains(s)));
            var hrs = await hrQuery.ToListAsync();
            contacts.AddRange(hrs.Select(h => (h.UserId, h.FirstName, h.MiddleName, h.LastName, h.Department?.Name, h.Position?.Name, (string?)null)));

            // ── Executives ──
            var execQuery = _db.Executives.Include(e => e.Department).Include(e => e.Position)
                .Where(e => e.UserId != currentUserId && !superAdminUserIds.Contains(e.UserId));
            if (!string.IsNullOrEmpty(s))
                execQuery = execQuery.Where(e => e.FirstName.ToLower().Contains(s) || e.LastName.ToLower().Contains(s) || (e.MiddleName != null && e.MiddleName.ToLower().Contains(s)));
            var execs = await execQuery.ToListAsync();
            contacts.AddRange(execs.Select(e => (e.UserId, e.FirstName, e.MiddleName, e.LastName, e.Department?.Name, e.Position?.Name, (string?)null)));

            // Deduplicate by UserId (a person might exist in multiple tables)
            var uniqueContacts = contacts
                .GroupBy(c => c.UserId)
                .Select(g => g.First())
                .OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
                .ToList();

            // Get roles for each contact
            var result = new List<object>();
            foreach (var c in uniqueContacts)
            {
                var user = await _userManager.FindByIdAsync(c.UserId);
                var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
                var role = roles.FirstOrDefault(r => r != "SuperAdmin") ?? "Employee";

                result.Add(new
                {
                    userId = c.UserId,
                    name = $"{c.FirstName} {c.LastName}",
                    initials = $"{c.FirstName[0]}{c.LastName[0]}".ToUpper(),
                    role,
                    department = c.DepartmentName ?? "",
                    position = c.PositionName ?? "",
                    email = c.Email ?? ""
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

                // Get display info from ALL role tables
                var otherUserIds = otherParticipants.Select(p => p.UserId).ToList();
                var otherUsers = await ResolveUsersInfoAsync(otherUserIds);

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
                    var otherInfo = otherUsers.Values.FirstOrDefault();
                    displayName = otherInfo?.FullName ?? "Unknown";
                    initials = otherInfo?.Initials ?? "??";
                }

                // Unread count
                var unreadCount = await _db.ChatMessages
                    .Where(m => m.ConversationId == conv.Id
                                && m.SenderId != currentUserId
                                && (cp.LastReadAt == null || m.SentAt > cp.LastReadAt))
                    .CountAsync();

                // Resolve last message sender's name from all role tables
                string? lastMsgSenderName = null;
                if (lastMessage != null)
                {
                    if (lastMessage.SenderId == currentUserId)
                    {
                        lastMsgSenderName = "You";
                    }
                    else
                    {
                        var senderInfo = await ResolveUserInfoAsync(lastMessage.SenderId);
                        lastMsgSenderName = senderInfo?.FullName
                            ?? lastMessage.Sender?.UserName ?? "Unknown";
                    }
                }

                result.Add(new
                {
                    id = conv.Id,
                    displayName,
                    initials,
                    isGroup = conv.IsGroup,
                    createdByUserId = conv.CreatedByUserId,
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

        // ───────── CREATE GROUP CONVERSATION ─────────

        [HttpPost("conversations/group")]
        public async Task<IActionResult> CreateGroupConversation([FromBody] GroupConversationRequest request)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            if (request.ParticipantIds == null || request.ParticipantIds.Count < 2)
                return BadRequest("A group chat requires at least 2 other members.");

            if (string.IsNullOrWhiteSpace(request.GroupName))
                return BadRequest("Group name is required.");

            // Ensure the current user is not in the participant list (we add them automatically)
            var memberIds = request.ParticipantIds
                .Where(id => id != currentUserId)
                .Distinct()
                .ToList();

            if (memberIds.Count < 2)
                return BadRequest("A group chat requires at least 2 other members.");

            var conversation = new Conversation
            {
                IsGroup = true,
                GroupName = request.GroupName.Trim(),
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();

            // Add the creator + all selected members
            var participants = new List<ConversationParticipant>
            {
                new() { ConversationId = conversation.Id, UserId = currentUserId }
            };
            participants.AddRange(memberIds.Select(id => new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = id
            }));

            _db.ConversationParticipants.AddRange(participants);
            await _db.SaveChangesAsync();

            // Notify all members via SignalR so their conversation list refreshes
            foreach (var pid in memberIds)
            {
                await _chatHub.Clients.Group($"user_{pid}")
                    .SendAsync("ConversationUpdated", new { conversationId = conversation.Id });
            }

            var initials = conversation.GroupName.Length >= 2
                ? conversation.GroupName[..2].ToUpper()
                : conversation.GroupName.ToUpper();

            return Ok(new
            {
                conversationId = conversation.Id,
                groupName = conversation.GroupName,
                initials
            });
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

            // Get user info for senders from all role tables
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senderInfoMap = await ResolveUsersInfoAsync(senderIds);

            var result = messages.OrderBy(m => m.SentAt).Select(m =>
            {
                senderInfoMap.TryGetValue(m.SenderId, out var info);
                return new
                {
                    id = m.Id,
                    conversationId = m.ConversationId,
                    senderId = m.SenderId,
                    senderName = info?.FullName ?? "Unknown",
                    senderInitials = info?.Initials ?? "??",
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

            // Get sender info from all role tables
            var senderInfo = await ResolveUserInfoAsync(currentUserId);
            var senderName = senderInfo?.FullName ?? "Unknown";
            var senderInitials = senderInfo?.Initials ?? "??";

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

        // ───────── GROUP DETAILS ─────────

        [HttpGet("conversations/{conversationId}/details")]
        public async Task<IActionResult> GetConversationDetails(int conversationId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var isParticipant = await _db.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUserId);
            if (!isParticipant) return Forbid();

            var conv = await _db.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conv == null) return NotFound();

            var participantIds = conv.Participants.Select(p => p.UserId).ToList();
            var userInfoMap = await ResolveUsersInfoAsync(participantIds);

            var members = conv.Participants.Select(p =>
            {
                userInfoMap.TryGetValue(p.UserId, out var info);
                return new
                {
                    userId = p.UserId,
                    name = info?.FullName ?? "Unknown",
                    initials = info?.Initials ?? "??",
                    department = info?.Department ?? "",
                    position = info?.Position ?? "",
                    joinedAt = p.JoinedAt
                };
            }).ToList();

            return Ok(new
            {
                id = conv.Id,
                isGroup = conv.IsGroup,
                groupName = conv.GroupName,
                createdByUserId = conv.CreatedByUserId,
                isCreator = conv.CreatedByUserId == currentUserId,
                members
            });
        }

        // ───────── LEAVE GROUP ─────────

        [HttpPost("conversations/{conversationId}/leave")]
        public async Task<IActionResult> LeaveGroup(int conversationId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var conv = await _db.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conv == null) return NotFound();
            if (!conv.IsGroup) return BadRequest("Cannot leave a direct conversation.");

            var participant = conv.Participants.FirstOrDefault(p => p.UserId == currentUserId);
            if (participant == null) return NotFound("You are not in this group.");

            _db.ConversationParticipants.Remove(participant);
            conv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify remaining members
            var remainingIds = conv.Participants
                .Where(p => p.UserId != currentUserId)
                .Select(p => p.UserId).ToList();
            foreach (var pid in remainingIds)
            {
                await _chatHub.Clients.Group($"user_{pid}")
                    .SendAsync("ConversationUpdated", new { conversationId });
            }

            return Ok();
        }

        // ───────── RENAME GROUP ─────────

        [HttpPut("conversations/{conversationId}/rename")]
        public async Task<IActionResult> RenameGroup(int conversationId, [FromBody] RenameGroupRequest request)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.GroupName))
                return BadRequest("Group name is required.");

            var conv = await _db.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conv == null) return NotFound();
            if (!conv.IsGroup) return BadRequest("Cannot rename a direct conversation.");

            var isParticipant = conv.Participants.Any(p => p.UserId == currentUserId);
            if (!isParticipant) return Forbid();

            conv.GroupName = request.GroupName.Trim();
            conv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify all participants
            foreach (var p in conv.Participants)
            {
                await _chatHub.Clients.Group($"user_{p.UserId}")
                    .SendAsync("ConversationUpdated", new { conversationId });
            }

            return Ok(new { groupName = conv.GroupName });
        }

        // ───────── REMOVE MEMBER FROM GROUP (creator only) ─────────

        [HttpDelete("conversations/{conversationId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int conversationId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var conv = await _db.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conv == null) return NotFound();
            if (!conv.IsGroup) return BadRequest("Cannot remove members from a direct conversation.");

            // Only the creator can remove members
            if (conv.CreatedByUserId != currentUserId)
                return Forbid();

            if (userId == currentUserId)
                return BadRequest("Use the leave endpoint to leave the group.");

            var participant = conv.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant == null) return NotFound("User is not in this group.");

            _db.ConversationParticipants.Remove(participant);
            conv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify the removed user
            await _chatHub.Clients.Group($"user_{userId}")
                .SendAsync("RemovedFromGroup", new { conversationId });

            // Notify remaining participants
            var remainingIds = conv.Participants
                .Where(p => p.UserId != userId)
                .Select(p => p.UserId).ToList();
            foreach (var pid in remainingIds)
            {
                await _chatHub.Clients.Group($"user_{pid}")
                    .SendAsync("ConversationUpdated", new { conversationId });
            }

            return Ok();
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

        public class GroupConversationRequest
        {
            public string GroupName { get; set; } = string.Empty;
            public List<string> ParticipantIds { get; set; } = new();
        }

        public class SendMessageRequest
        {
            public string Content { get; set; } = string.Empty;
        }

        public class RenameGroupRequest
        {
            public string GroupName { get; set; } = string.Empty;
        }

        // ───────── HELPERS ─────────

        /// <summary>
        /// Resolves user display info (name, initials, role, department, position) by checking
        /// all role tables: Employees, ProjectManagers, DepartmentHeads, HumanResources, Executives.
        /// </summary>
        private async Task<UserDisplayInfo?> ResolveUserInfoAsync(string userId)
        {
            // Check Employee table
            var emp = await _db.Employees
                .Include(e => e.Department).Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);
            if (emp != null)
                return new UserDisplayInfo(emp.FirstName, emp.LastName, emp.Department?.Name, emp.Position?.Name);

            // Check ProjectManager table
            var pm = await _db.ProjectManagers
                .Include(p => p.Department).Include(p => p.Position)
                .FirstOrDefaultAsync(p => p.UserId == userId);
            if (pm != null)
                return new UserDisplayInfo(pm.FirstName, pm.LastName, pm.Department?.Name, pm.Position?.Name);

            // Check DepartmentHead table
            var dh = await _db.DepartmentHeads
                .Include(d => d.Department).Include(d => d.Position)
                .FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh != null)
                return new UserDisplayInfo(dh.FirstName, dh.LastName, dh.Department?.Name, dh.Position?.Name);

            // Check HumanResource table
            var hr = await _db.HumanResources
                .Include(h => h.Department).Include(h => h.Position)
                .FirstOrDefaultAsync(h => h.UserId == userId);
            if (hr != null)
                return new UserDisplayInfo(hr.FirstName, hr.LastName, hr.Department?.Name, hr.Position?.Name);

            // Check Executive table
            var exec = await _db.Executives
                .Include(e => e.Department).Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);
            if (exec != null)
                return new UserDisplayInfo(exec.FirstName, exec.LastName, exec.Department?.Name, exec.Position?.Name);

            return null;
        }

        /// <summary>
        /// Batch-resolves user display info for multiple user IDs.
        /// </summary>
        private async Task<Dictionary<string, UserDisplayInfo>> ResolveUsersInfoAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            var result = new Dictionary<string, UserDisplayInfo>();

            // Employees
            var employees = await _db.Employees
                .Include(e => e.Department).Include(e => e.Position)
                .Where(e => ids.Contains(e.UserId))
                .ToListAsync();
            foreach (var e in employees)
                result[e.UserId] = new UserDisplayInfo(e.FirstName, e.LastName, e.Department?.Name, e.Position?.Name);

            var remaining = ids.Except(result.Keys).ToList();
            if (remaining.Count == 0) return result;

            // ProjectManagers
            var pms = await _db.ProjectManagers
                .Include(p => p.Department).Include(p => p.Position)
                .Where(p => remaining.Contains(p.UserId))
                .ToListAsync();
            foreach (var p in pms)
                result[p.UserId] = new UserDisplayInfo(p.FirstName, p.LastName, p.Department?.Name, p.Position?.Name);

            remaining = ids.Except(result.Keys).ToList();
            if (remaining.Count == 0) return result;

            // DepartmentHeads
            var dhs = await _db.DepartmentHeads
                .Include(d => d.Department).Include(d => d.Position)
                .Where(d => remaining.Contains(d.UserId))
                .ToListAsync();
            foreach (var d in dhs)
                result[d.UserId] = new UserDisplayInfo(d.FirstName, d.LastName, d.Department?.Name, d.Position?.Name);

            remaining = ids.Except(result.Keys).ToList();
            if (remaining.Count == 0) return result;

            // HumanResources
            var hrs = await _db.HumanResources
                .Include(h => h.Department).Include(h => h.Position)
                .Where(h => remaining.Contains(h.UserId))
                .ToListAsync();
            foreach (var h in hrs)
                result[h.UserId] = new UserDisplayInfo(h.FirstName, h.LastName, h.Department?.Name, h.Position?.Name);

            remaining = ids.Except(result.Keys).ToList();
            if (remaining.Count == 0) return result;

            // Executives
            var execs = await _db.Executives
                .Include(e => e.Department).Include(e => e.Position)
                .Where(e => remaining.Contains(e.UserId))
                .ToListAsync();
            foreach (var e in execs)
                result[e.UserId] = new UserDisplayInfo(e.FirstName, e.LastName, e.Department?.Name, e.Position?.Name);

            return result;
        }

        private record UserDisplayInfo(string FirstName, string LastName, string? Department, string? Position)
        {
            public string FullName => $"{FirstName} {LastName}";
            public string Initials => $"{FirstName[0]}{LastName[0]}".ToUpper();
        }

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
