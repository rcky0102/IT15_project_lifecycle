using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;

namespace project_lifecycle.DepartmentHeadArea.Controllers
{
    [Area("DepartmentHead")]
    [Authorize(Roles = "DepartmentHead")]
    public class ProposalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProposalController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
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

            var proposals = await _context.ProjectProposals
                .Include(p => p.Employee)
                .Where(p => p.Employee != null && p.Employee.DepartmentId == dh.DepartmentId)
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync();

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
            return RedirectToAction(nameof(Details), new { id = proposal.Id });
        }
    }
}
