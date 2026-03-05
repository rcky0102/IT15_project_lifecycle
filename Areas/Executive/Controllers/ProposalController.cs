using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;

namespace project_lifecycle.ExecutiveArea.Controllers
{
    [Area("Executive")]
    [Authorize(Roles = "Executive")]
    public class ProposalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProposalController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string archiveFilter = "active")
        {
            ViewData["Title"] = "Proposals";

            IQueryable<ProjectProposal> query = _context.ProjectProposals
                .Include(p => p.Employee);

            if (!string.Equals(archiveFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(archiveFilter, "inactive", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p => p.IsArchived);
                }
                else
                {
                    query = query.Where(p => !p.IsArchived);
                }
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
    }
}
