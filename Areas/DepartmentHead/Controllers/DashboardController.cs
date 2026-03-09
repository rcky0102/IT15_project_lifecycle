using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using project_lifecycle.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace project_lifecycle.DepartmentHeadArea.Controllers
{
    [Area("DepartmentHead")]
    [Authorize(Roles = "DepartmentHead")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            int departmentId = 0;
            string departmentName = "Department";
            string dhName = "Department Head";

            if (!string.IsNullOrEmpty(userId))
            {
                var dh = await _context.DepartmentHeads
                    .Include(d => d.Department)
                    .FirstOrDefaultAsync(d => d.UserId == userId);

                if (dh != null)
                {
                    departmentId = dh.DepartmentId;
                    departmentName = dh.Department?.Name ?? "Department";
                    dhName = $"{dh.FirstName} {dh.LastName}";
                }
            }

            // Employees in this department
            var deptEmpIds = _context.Employees
                .Where(e => e.DepartmentId == departmentId)
                .Select(e => e.Id);

            int totalEmployees = await deptEmpIds.CountAsync();

            // Projects where at least one dept employee is a member (non-archived)
            var deptProjects = _context.Projects
                .Where(p => !p.IsArchived && _context.Members.Any(m => m.ProjectId == p.Id && deptEmpIds.Contains(m.EmployeeId)));

            var deptProjectIds = deptProjects.Select(p => p.Id);

            int activeProjects = await deptProjects.CountAsync(p => p.Status == "Unfinished");
            int finishedProjects = await deptProjects.CountAsync(p => p.Status == "Finished");

            // Proposals from dept employees (not archived)
            int totalProposals = await _context.ProjectProposals
                .Where(pp => deptEmpIds.Contains(pp.EmployeeId) && !pp.IsArchived)
                .CountAsync();

            int pendingProposals = await _context.ProjectProposals
                .Where(pp => deptEmpIds.Contains(pp.EmployeeId) && pp.Status == "Pending" && !pp.IsArchived)
                .CountAsync();

            // Open tasks (Pending) across department projects
            int openTasks = await _context.ProjectTasks
                .Where(t => !t.IsArchived && t.Status == "Pending"
                    && _context.ProjectMilestones.Any(pm => pm.Id == t.ProjectMilestoneId && deptProjectIds.Contains(pm.ProjectId)))
                .CountAsync();

            // Tasks checked/completed this month
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            int completedTasksThisMonth = await _context.ProjectTasks
                .Where(t => !t.IsArchived && t.Status == "Checked"
                    && t.CompletedAt.HasValue && t.CompletedAt.Value >= monthStart
                    && _context.ProjectMilestones.Any(pm => pm.Id == t.ProjectMilestoneId && deptProjectIds.Contains(pm.ProjectId)))
                .CountAsync();

            ViewData["DepartmentName"] = departmentName;
            ViewData["DHName"] = dhName;
            ViewData["TotalEmployees"] = totalEmployees;
            ViewData["ActiveProjects"] = activeProjects;
            ViewData["FinishedProjects"] = finishedProjects;
            ViewData["PendingProposals"] = pendingProposals;
            ViewData["TotalProposals"] = totalProposals;
            ViewData["OpenTasks"] = openTasks;
            ViewData["CompletedTasksThisMonth"] = completedTasksThisMonth;

            return View();
        }

        // Proposal status distribution for doughnut chart
        [HttpGet]
        public async Task<IActionResult> ProposalStatusData()
        {
            var userId = _userManager.GetUserId(User);
            int departmentId = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
                if (dh != null) departmentId = dh.DepartmentId;
            }

            var deptEmpIds = _context.Employees
                .Where(e => e.DepartmentId == departmentId)
                .Select(e => e.Id);

            var statusCounts = await _context.ProjectProposals
                .Where(pp => deptEmpIds.Contains(pp.EmployeeId) && !pp.IsArchived)
                .GroupBy(pp => pp.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            // Ensure all statuses are represented
            var allStatuses = new[] { "Pending", "Approved", "Rejected", "Requires Revision" };
            var result = allStatuses.Select(s => new
            {
                status = s,
                count = statusCounts.FirstOrDefault(x => x.status == s)?.count ?? 0
            });

            return Json(result);
        }

        // Task completion trend for line chart
        [HttpGet]
        public async Task<IActionResult> TaskCompletionTrend(int days = 7)
        {
            var userId = _userManager.GetUserId(User);
            int departmentId = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
                if (dh != null) departmentId = dh.DepartmentId;
            }

            var deptEmpIds = _context.Employees
                .Where(e => e.DepartmentId == departmentId)
                .Select(e => e.Id);

            var deptProjectIds = _context.Projects
                .Where(p => !p.IsArchived && _context.Members.Any(m => m.ProjectId == p.Id && deptEmpIds.Contains(m.EmployeeId)))
                .Select(p => p.Id);

            var startDate = DateTime.UtcNow.Date.AddDays(-(days - 1));

            var counts = await _context.ProjectTasks
                .Where(t => !t.IsArchived && t.Status == "Checked"
                    && t.CompletedAt.HasValue
                    && t.CompletedAt.Value.Date >= startDate
                    && _context.ProjectMilestones.Any(pm => pm.Id == t.ProjectMilestoneId && deptProjectIds.Contains(pm.ProjectId)))
                .GroupBy(t => new { Year = t.CompletedAt!.Value.Year, Month = t.CompletedAt.Value.Month, Day = t.CompletedAt.Value.Day })
                .Select(g => new { Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day), Count = g.Count() })
                .ToListAsync();

            var labels = Enumerable.Range(0, days).Select(i => startDate.AddDays(i)).ToList();
            var data = labels.Select(d => counts.FirstOrDefault(c => c.Date == d)?.Count ?? 0).ToList();

            return Json(new { labels = labels.Select(d => d.ToString("yyyy-MM-dd")), data });
        }

        // Finished projects trend for line chart (grouped by EndDate)
        [HttpGet]
        public async Task<IActionResult> FinishedProjectsTrend(int days = 7)
        {
            var userId = _userManager.GetUserId(User);
            int departmentId = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
                if (dh != null) departmentId = dh.DepartmentId;
            }

            var deptEmpIds = _context.Employees
                .Where(e => e.DepartmentId == departmentId)
                .Select(e => e.Id);

            var startDate = DateTime.UtcNow.Date.AddDays(-(days - 1));

            var counts = await _context.Projects
                .Where(p => !p.IsArchived && p.Status == "Finished"
                    && p.EndDate.Date >= startDate
                    && p.EndDate.Date <= DateTime.UtcNow.Date
                    && _context.Members.Any(m => m.ProjectId == p.Id && deptEmpIds.Contains(m.EmployeeId)))
                .GroupBy(p => new { Year = p.EndDate.Year, Month = p.EndDate.Month, Day = p.EndDate.Day })
                .Select(g => new { Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day), Count = g.Count() })
                .ToListAsync();

            var labels = Enumerable.Range(0, days).Select(i => startDate.AddDays(i)).ToList();
            var data = labels.Select(d => counts.FirstOrDefault(c => c.Date == d)?.Count ?? 0).ToList();

            return Json(new { labels = labels.Select(d => d.ToString("yyyy-MM-dd")), data });
        }
    }
}

