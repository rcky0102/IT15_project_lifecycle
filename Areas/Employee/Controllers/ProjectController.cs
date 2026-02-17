using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.ViewModels.ProjectManager;

namespace project_lifecycle.EmployeeArea.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "My Projects";

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return View(new ProjectIndexViewModel());

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return View(new ProjectIndexViewModel());

            var projects = await _context.Projects
                .Where(p => _context.Members.Any(m => m.ProjectId == p.Id && m.EmployeeId == employee.Id))
                .OrderByDescending(p => p.DateCreated)
                .Select(p => new ProjectDetailViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProposalTitle = p.ProjectProposal != null ? p.ProjectProposal.Title : string.Empty,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .ToListAsync();

            var projectIds = projects.Select(p => p.Id).ToList();

            var members = await _context.Members
                .Where(m => projectIds.Contains(m.ProjectId))
                .Include(m => m.Employee)
                .Include(m => m.ProjectRole)
                .ToListAsync();

            var milestones = await _context.ProjectMilestones
                .Where(pm => projectIds.Contains(pm.ProjectId))
                .Include(pm => pm.Milestone)
                .OrderBy(pm => pm.SequenceOrder)
                .ToListAsync();

            foreach (var project in projects)
            {
                project.Members = members
                    .Where(m => m.ProjectId == project.Id)
                    .Select(m => new MemberViewModel
                    {
                        Id = m.Id,
                        EmployeeId = m.EmployeeId,
                        EmployeeName = m.Employee != null ? string.Join(" ", new[] { m.Employee.FirstName, m.Employee.MiddleName, m.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : "N/A",
                        ProjectRoleId = m.ProjectRoleId,
                        ProjectRoleName = m.ProjectRole != null ? m.ProjectRole.Name : "N/A"
                    })
                    .ToList();

                project.Milestones = milestones
                    .Where(ms => ms.ProjectId == project.Id)
                    .Select(ms => new ProjectMilestoneViewModel
                    {
                        Id = ms.Id,
                        MilestoneId = ms.MilestoneId,
                        MilestoneName = ms.Milestone != null ? ms.Milestone.Name : "N/A",
                        SequenceOrder = ms.SequenceOrder,
                        Status = ms.Status
                    })
                    .ToList();
            }

            var vm = new ProjectIndexViewModel { Projects = projects };
            return View(vm);
        }
    }
}
