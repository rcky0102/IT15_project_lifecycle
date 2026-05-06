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
    public class GroupsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<ChatHub> _chatHub;

        public GroupsController(ApplicationDbContext db, UserManager<IdentityUser> userManager, IHubContext<ChatHub> chatHub)
        {
            _db = db;
            _userManager = userManager;
            _chatHub = chatHub;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroup(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var conv = await _db.Conversations
                .Include(c => c.Participants)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsGroup);
            if (conv == null) return NotFound();

            var isParticipant = conv.Participants.Any(p => p.UserId == currentUserId);
            if (!isParticipant) return Forbid();

            var members = new List<object>();
            foreach (var p in conv.Participants)
            {
                var info = await ResolveUserInfoAsync(p.UserId);
                members.Add(new { id = p.UserId, name = info?.FullName ?? p.UserId });
            }

            return Ok(new { id = conv.Id, name = conv.GroupName, members });
        }

        public class MemberChangeRequest { public string MemberId { get; set; } = string.Empty; }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequest req)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Group name required");
            var memberIds = req.Members?.Where(m => m != currentUserId).Distinct().ToList() ?? new List<string>();
            if (memberIds.Count < 1) return BadRequest("At least one other member required");

            var conv = new Conversation { IsGroup = true, GroupName = req.Name.Trim(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _db.Conversations.Add(conv);
            await _db.SaveChangesAsync();

            var participants = new List<ConversationParticipant> { new() { ConversationId = conv.Id, UserId = currentUserId } };
            participants.AddRange(memberIds.Select(id => new ConversationParticipant { ConversationId = conv.Id, UserId = id }));
            _db.ConversationParticipants.AddRange(participants);
            await _db.SaveChangesAsync();

            foreach (var pid in memberIds) await _chatHub.Clients.Group($"user_{pid}").SendAsync("ConversationUpdated", new { conversationId = conv.Id });

            return Ok(new { success = true, groupId = conv.Id, name = conv.GroupName });
        }

        [HttpPost("{id}/members/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int id, [FromBody] MemberChangeRequest req)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var conv = await _db.Conversations.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == id && c.IsGroup);
            if (conv == null) return NotFound();
            if (!conv.Participants.Any(p => p.UserId == currentUserId)) return Forbid();

            if (conv.Participants.Any(p => p.UserId == req.MemberId)) return BadRequest("Already a member");

            _db.ConversationParticipants.Add(new ConversationParticipant { ConversationId = conv.Id, UserId = req.MemberId });
            conv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var info = await ResolveUserInfoAsync(req.MemberId);
            await _chatHub.Clients.Group($"user_{req.MemberId}").SendAsync("ConversationUpdated", new { conversationId = conv.Id });
            return Ok(new { success = true, name = info?.FullName });
        }

        [HttpPost("{id}/members/remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int id, [FromBody] MemberChangeRequest req)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var conv = await _db.Conversations.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == id && c.IsGroup);
            if (conv == null) return NotFound();
            if (!conv.Participants.Any(p => p.UserId == currentUserId)) return Forbid();

            var part = conv.Participants.FirstOrDefault(p => p.UserId == req.MemberId);
            if (part == null) return NotFound();

            _db.ConversationParticipants.Remove(part);
            conv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _chatHub.Clients.Group($"user_{req.MemberId}").SendAsync("ConversationUpdated", new { conversationId = conv.Id });
            return Ok(new { success = true });
        }

        [HttpPost("{id}/rename")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(int id, [FromBody] RenameRequest req)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var conv = await _db.Conversations.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == id && c.IsGroup);
            if (conv == null) return NotFound();
            if (!conv.Participants.Any(p => p.UserId == currentUserId)) return Forbid();

            conv.GroupName = req.Name?.Trim();
            conv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify participants
            var participantIds = conv.Participants.Select(p => p.UserId).ToList();
            foreach (var pid in participantIds) await _chatHub.Clients.Group($"user_{pid}").SendAsync("ConversationUpdated", new { conversationId = conv.Id });

            return Ok(new { success = true });
        }

        [HttpPost("{id}/leave")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var conv = await _db.Conversations.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == id && c.IsGroup);
            if (conv == null) return NotFound();

            var part = conv.Participants.FirstOrDefault(p => p.UserId == currentUserId);
            if (part == null) return NotFound();

            _db.ConversationParticipants.Remove(part);
            conv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify remaining participants
            foreach (var pid in conv.Participants.Select(p => p.UserId))
                await _chatHub.Clients.Group($"user_{pid}").SendAsync("ConversationUpdated", new { conversationId = conv.Id });

            return Ok(new { success = true });
        }

        private async Task<UserDisplayInfo?> ResolveUserInfoAsync(string userId)
        {
            var emp = await _db.Employees.Include(e => e.Department).Include(e => e.Position).FirstOrDefaultAsync(e => e.UserId == userId);
            if (emp != null) return new UserDisplayInfo(emp.FirstName, emp.LastName);

            var pm = await _db.ProjectManagers.Include(p => p.Department).Include(p => p.Position).FirstOrDefaultAsync(p => p.UserId == userId);
            if (pm != null) return new UserDisplayInfo(pm.FirstName, pm.LastName);

            var dh = await _db.DepartmentHeads.Include(d => d.Department).Include(d => d.Position).FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh != null) return new UserDisplayInfo(dh.FirstName, dh.LastName);

            var hr = await _db.HumanResources.Include(h => h.Department).Include(h => h.Position).FirstOrDefaultAsync(h => h.UserId == userId);
            if (hr != null) return new UserDisplayInfo(hr.FirstName, hr.LastName);

            var ex = await _db.Executives.Include(e => e.Department).Include(e => e.Position).FirstOrDefaultAsync(e => e.UserId == userId);
            if (ex != null) return new UserDisplayInfo(ex.FirstName, ex.LastName);

            return null;
        }

        private record UserDisplayInfo(string FirstName, string LastName)
        {
            public string FullName => $"{FirstName} {LastName}";
        }

        public class CreateGroupRequest { public string Name { get; set; } = string.Empty; public List<string> Members { get; set; } = new(); }
        public class RenameRequest { public string Name { get; set; } = string.Empty; }
    }
}
