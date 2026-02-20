using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using project_lifecycle.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace project_lifecycle.EmployeeArea.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        [ActivatorUtilitiesConstructor]
        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Default to global counts in case we cannot resolve the current employee
            int activeProjects = 0;
            int openTasks = 0;

            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee != null)
                {
                    // Projects where this employee is a member
                    activeProjects = _context.Projects.Count(p => _context.Members.Any(m => m.ProjectId == p.Id && m.EmployeeId == employee.Id));

                    // Open tasks assigned to this employee via TaskMembers and not Checked
                    openTasks = _context.TaskMembers
                        .Where(tm => tm.Member != null && tm.Member.EmployeeId == employee.Id && tm.ProjectTask != null && tm.ProjectTask.Status != "Checked")
                        .Select(tm => tm.ProjectTaskId)
                        .Distinct()
                        .Count();
                }
            }

            ViewData["ActiveProjects"] = activeProjects;
            ViewData["OpenTasks"] = openTasks;

            return View();
        }
    }
}
