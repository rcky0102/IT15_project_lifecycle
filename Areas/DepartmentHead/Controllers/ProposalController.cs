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

            return View(proposal);
        }
    }
}
