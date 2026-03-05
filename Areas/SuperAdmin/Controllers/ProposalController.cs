using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class ProposalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _audit;

        public ProposalController(ApplicationDbContext context, IAuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string archiveFilter = "active")
        {
            ViewData["Title"] = "Proposals";

            IQueryable<ProjectProposal> query = _context.ProjectProposals
                .Include(p => p.Employee);

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
            var proposal = await _context.ProjectProposals
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null) return NotFound();

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
            var version = await _context.ProjectProposalVersions
                .Include(v => v.ProjectProposal)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();

            return View(version);
        }

        [HttpGet]
        public async Task<IActionResult> Note(int id)
        {
            var note = await _context.ProposalNoteVersions
                .Include(n => n.ProjectProposal)
                    .ThenInclude(p => p.Employee)
                .Include(n => n.DepartmentHead)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null) return NotFound();

            return View(note);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var proposal = await _context.ProjectProposals.FindAsync(id);
            if (proposal == null) return NotFound();

            proposal.IsArchived = true;
            _context.Update(proposal);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Archive", "Proposals", $"Archived proposal '{proposal.Title}' (ID: {proposal.Id})", "ProjectProposal", proposal.Id.ToString());

            TempData["SuccessMessage"] = "Proposal archived.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            var proposal = await _context.ProjectProposals.FindAsync(id);
            if (proposal == null) return NotFound();

            proposal.IsArchived = false;
            _context.Update(proposal);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Unarchive", "Proposals", $"Unarchived proposal '{proposal.Title}' (ID: {proposal.Id})", "ProjectProposal", proposal.Id.ToString());

            TempData["SuccessMessage"] = "Proposal restored.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var proposal = await _context.ProjectProposals.FindAsync(id);
            if (proposal == null) return NotFound();

            var title = proposal.Title;

            // Remove related note versions
            var noteVersions = await _context.ProposalNoteVersions
                .Where(n => n.ProjectProposalId == id).ToListAsync();
            _context.ProposalNoteVersions.RemoveRange(noteVersions);

            // Remove related proposal versions
            var proposalVersions = await _context.ProjectProposalVersions
                .Where(v => v.ProjectProposalId == id).ToListAsync();
            _context.ProjectProposalVersions.RemoveRange(proposalVersions);

            _context.ProjectProposals.Remove(proposal);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Delete", "Proposals", $"Deleted proposal '{title}' (ID: {id})", "ProjectProposal", id.ToString());

            TempData["SuccessMessage"] = "Proposal permanently deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
