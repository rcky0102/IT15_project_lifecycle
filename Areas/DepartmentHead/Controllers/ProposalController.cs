using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;

namespace project_lifecycle.DepartmentHeadArea.Controllers
{
    [Area("DepartmentHead")]
    [Authorize(Roles = "DepartmentHead")]
    public class ProposalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _audit;
        private readonly INotificationService _notif;

        public ProposalController(ApplicationDbContext context, IAuditLogService audit, INotificationService notif)
        {
            _context = context;
            _audit = audit;
            _notif = notif;
        }

        [HttpGet]
        [Route("DepartmentHead/Proposal")]
        [Route("DepartmentHead/Proposal/Index")]
        public async Task<IActionResult> Index(string archiveFilter = "active")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh == null)
            {
                return Forbid();
            }

            IQueryable<ProjectProposal> query = _context.ProjectProposals
                .Include(p => p.Employee)
                .Where(p => p.Employee != null && p.Employee.DepartmentId == dh.DepartmentId);

            if (!string.Equals(archiveFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(archiveFilter, "inactive", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => p.IsArchived);
                else
                    query = query.Where(p => !p.IsArchived);
            }

            var proposals = await query.OrderByDescending(p => p.DateCreated).ToListAsync();
            ViewData["ArchiveFilter"] = archiveFilter;

            return View(proposals);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh == null) return Forbid();

            var proposal = await _context.ProjectProposals
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null) return NotFound();

            if (proposal.Employee == null || proposal.Employee.DepartmentId != dh.DepartmentId)
                return Forbid();

            var versions = await _context.ProjectProposalVersions
                .Where(v => v.ProjectProposalId == proposal.Id)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

            ViewBag.ProjectProposalVersions = versions;

            var noteVersions = await _context.ProposalNoteVersions
                .Where(n => n.ProjectProposalId == proposal.Id)
                .OrderByDescending(n => n.VersionNumber)
                .ToListAsync();

            ViewBag.ProposalNoteVersions = noteVersions;

            return View(proposal);
        }

        [HttpGet]
        public async Task<IActionResult> Version(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh == null) return Forbid();

            var version = await _context.ProjectProposalVersions
                .Include(v => v.ProjectProposal)
                .ThenInclude(p => p.Employee)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();

            if (version.ProjectProposal == null || version.ProjectProposal.Employee == null || version.ProjectProposal.Employee.DepartmentId != dh.DepartmentId)
                return Forbid();

            return View(version);
        }

        [HttpGet]
        public async Task<IActionResult> Note(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh == null) return Forbid();

            var note = await _context.ProposalNoteVersions
                .Include(n => n.ProjectProposal)
                .ThenInclude(p => p.Employee)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null) return NotFound();

            if (note.ProjectProposal == null || note.ProjectProposal.Employee == null || note.ProjectProposal.Employee.DepartmentId != dh.DepartmentId)
                return Forbid();

            return View(note);
        }

        [HttpGet]
        public async Task<IActionResult> Notes(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh == null) return Forbid();

            var proposal = await _context.ProjectProposals
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null) return NotFound();

            if (proposal.Employee == null || proposal.Employee.DepartmentId != dh.DepartmentId)
                return Forbid();

            var notes = await _context.ProposalNoteVersions
                .Where(n => n.ProjectProposalId == proposal.Id)
                .OrderByDescending(n => n.DateCreated)
                .ToListAsync();

            return View(notes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, string actionType, string? note)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh == null) return Forbid();

            var proposal = await _context.ProjectProposals
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null) return NotFound();

            if (proposal.Employee == null || proposal.Employee.DepartmentId != dh.DepartmentId)
                return Forbid();

            // Handle SendFeedback separately: save a note version
            if (string.Equals(actionType, "SendFeedback", StringComparison.OrdinalIgnoreCase))
            {
                // compute next version number
                var maxVersion = await _context.ProposalNoteVersions
                    .Where(n => n.ProjectProposalId == proposal.Id)
                    .MaxAsync(n => (int?)n.VersionNumber) ?? 0;

                var noteVersion = new ProposalNoteVersion
                {
                    ProjectProposalId = proposal.Id,
                    VersionNumber = maxVersion + 1,
                    Note = note,
                    DepartmentHeadId = dh.Id,
                    DateCreated = DateTime.Now
                };

                _context.ProposalNoteVersions.Add(noteVersion);

                // also update the current proposal's note and department head reference
                proposal.Note = note;
                proposal.DepartmentHeadId = dh.Id;

                _context.Update(proposal);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Map actionType to allowed statuses
                string newStatus = proposal.Status;
                if (string.Equals(actionType, "Approve", StringComparison.OrdinalIgnoreCase))
                {
                    newStatus = "Approved";
                }
                else if (string.Equals(actionType, "Reject", StringComparison.OrdinalIgnoreCase))
                {
                    newStatus = "Rejected";
                }
                else if (string.Equals(actionType, "ReturnForRevision", StringComparison.OrdinalIgnoreCase))
                {
                    newStatus = "Requires Revision";
                }

                // Save note and department head assignment
                proposal.Note = note;
                proposal.DepartmentHeadId = dh.Id;
                proposal.Status = newStatus;

                _context.Update(proposal);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Proposal updated.";
            await _audit.LogAsync(User, actionType, "Proposal Review", $"{actionType} proposal '{proposal.Title}' (ID: {proposal.Id})", "ProjectProposal", proposal.Id.ToString());

            // Notify the proposal's employee
            if (proposal.Employee != null && !string.IsNullOrEmpty(proposal.Employee.UserId))
            {
                var (notifTitle, notifMsg, notifType, notifIcon) = actionType?.ToLower() switch
                {
                    "approve" => ("Proposal Approved", $"Your proposal '{proposal.Title}' has been approved.", "Success", "fas fa-check-circle"),
                    "reject" => ("Proposal Rejected", $"Your proposal '{proposal.Title}' has been rejected.", "Error", "fas fa-times-circle"),
                    "returnforrevision" => ("Revision Required", $"Your proposal '{proposal.Title}' requires revision.", "Warning", "fas fa-exclamation-triangle"),
                    "sendfeedback" => ("New Feedback", $"You received feedback on your proposal '{proposal.Title}'.", "Info", "fas fa-comment-dots"),
                    _ => ("Proposal Updated", $"Your proposal '{proposal.Title}' has been updated.", "Info", "fas fa-info-circle")
                };

                await _notif.CreateAsync(
                    proposal.Employee.UserId,
                    notifTitle, notifMsg, notifType, notifIcon,
                    $"/Employee/Proposal/Details/{proposal.Id}",
                    "Proposal");
            }

            return RedirectToAction(nameof(Details), new { id = proposal.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh == null) return Forbid();

            var proposal = await _context.ProjectProposals
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null) return NotFound();
            if (proposal.Employee == null || proposal.Employee.DepartmentId != dh.DepartmentId) return Forbid();

            proposal.IsArchived = true;
            _context.Update(proposal);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Archive", "Proposals", $"Archived proposal '{proposal.Title}' (ID: {proposal.Id})", "ProjectProposal", proposal.Id.ToString());

            if (proposal.Employee != null && !string.IsNullOrEmpty(proposal.Employee.UserId))
            {
                await _notif.CreateAsync(proposal.Employee.UserId,
                    "Proposal Archived", $"Your proposal '{proposal.Title}' has been archived.",
                    "Warning", "fas fa-archive", $"/Employee/Proposal", "Proposal");
            }

            TempData["SuccessMessage"] = "Proposal archived.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dh == null) return Forbid();

            var proposal = await _context.ProjectProposals
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null) return NotFound();
            if (proposal.Employee == null || proposal.Employee.DepartmentId != dh.DepartmentId) return Forbid();

            proposal.IsArchived = false;
            _context.Update(proposal);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Unarchive", "Proposals", $"Unarchived proposal '{proposal.Title}' (ID: {proposal.Id})", "ProjectProposal", proposal.Id.ToString());

            if (proposal.Employee != null && !string.IsNullOrEmpty(proposal.Employee.UserId))
            {
                await _notif.CreateAsync(proposal.Employee.UserId,
                    "Proposal Restored", $"Your proposal '{proposal.Title}' has been unarchived.",
                    "Info", "fas fa-box-open", $"/Employee/Proposal/Details/{proposal.Id}", "Proposal");
            }

            TempData["SuccessMessage"] = "Proposal unarchived.";
            return RedirectToAction(nameof(Index));
        }
    }
}
