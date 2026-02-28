using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace project_lifecycle.ExecutiveArea.Controllers
{
    [Area("Executive")]
    [Authorize(Roles = "Executive")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ── Stat cards ──
            var totalProjects = await _context.Projects.CountAsync();
            var activeProjects = await _context.Projects.CountAsync(p => !p.IsArchived);
            var archivedProjects = await _context.Projects.CountAsync(p => p.IsArchived);
            var totalEmployees = await _context.Employees.CountAsync();
            var totalDepartments = await _context.Departments.CountAsync();
            var pendingProposals = await _context.ProjectProposals.CountAsync(pp => pp.Status == "Pending");
            var approvedProposals = await _context.ProjectProposals.CountAsync(pp => pp.Status == "Approved");
            var rejectedProposals = await _context.ProjectProposals.CountAsync(pp => pp.Status == "Rejected");
            var totalProposals = await _context.ProjectProposals.CountAsync();

            var totalTasks = await _context.ProjectTasks.CountAsync();
            var completedTasks = await _context.ProjectTasks.CountAsync(t => t.Status == "Checked");
            var pendingTasks = await _context.ProjectTasks.CountAsync(t => t.Status == "Pending");
            var revisionTasks = await _context.ProjectTasks.CountAsync(t => t.Status == "Require Revision");

            var totalMembers = await _context.Members.CountAsync();
            var totalProjectManagers = await _context.ProjectManagers.CountAsync();

            // ── Milestone stats ──
            var totalMilestones = await _context.ProjectMilestones.CountAsync();
            var finishedMilestones = await _context.ProjectMilestones.CountAsync(pm => pm.Status == "Finished");
            var unfinishedMilestones = await _context.ProjectMilestones.CountAsync(pm => pm.Status == "Unfinished");

            // ── Task completion rate ──
            var taskCompletionRate = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 1) : 0;
            var milestoneCompletionRate = totalMilestones > 0 ? Math.Round((double)finishedMilestones / totalMilestones * 100, 1) : 0;

            // ── Recent projects (last 5) ──
            var recentProjects = await _context.Projects
                .OrderByDescending(p => p.DateCreated)
                .Take(5)
                .Select(p => new { p.Name, p.DateCreated, p.IsArchived, p.StartDate, p.EndDate })
                .ToListAsync();

            // ── Recent proposals (last 5) ──
            var recentProposals = await _context.ProjectProposals
                .Include(pp => pp.Employee)
                .OrderByDescending(pp => pp.DateCreated)
                .Take(5)
                .Select(pp => new { pp.Title, pp.Status, pp.DateCreated, EmployeeName = pp.Employee != null ? pp.Employee.FirstName + " " + pp.Employee.LastName : "Unknown" })
                .ToListAsync();

            // ── Department breakdown ──
            var departmentBreakdown = await _context.Departments
                .Select(d => new
                {
                    d.Name,
                    EmployeeCount = _context.Employees.Count(e => e.DepartmentId == d.Id)
                })
                .OrderByDescending(d => d.EmployeeCount)
                .ToListAsync();

            // Pack into ViewData
            ViewData["TotalProjects"] = totalProjects;
            ViewData["ActiveProjects"] = activeProjects;
            ViewData["ArchivedProjects"] = archivedProjects;
            ViewData["TotalEmployees"] = totalEmployees;
            ViewData["TotalDepartments"] = totalDepartments;
            ViewData["PendingProposals"] = pendingProposals;
            ViewData["ApprovedProposals"] = approvedProposals;
            ViewData["RejectedProposals"] = rejectedProposals;
            ViewData["TotalProposals"] = totalProposals;
            ViewData["TotalTasks"] = totalTasks;
            ViewData["CompletedTasks"] = completedTasks;
            ViewData["PendingTasks"] = pendingTasks;
            ViewData["RevisionTasks"] = revisionTasks;
            ViewData["TotalMembers"] = totalMembers;
            ViewData["TotalProjectManagers"] = totalProjectManagers;
            ViewData["TotalMilestones"] = totalMilestones;
            ViewData["FinishedMilestones"] = finishedMilestones;
            ViewData["UnfinishedMilestones"] = unfinishedMilestones;
            ViewData["TaskCompletionRate"] = taskCompletionRate;
            ViewData["MilestoneCompletionRate"] = milestoneCompletionRate;
            ViewData["RecentProjects"] = recentProjects
                .Select(p => new { p.Name, DateCreated = p.DateCreated.ToString("MMM dd, yyyy"), p.IsArchived, StartDate = p.StartDate.ToString("MMM dd"), EndDate = p.EndDate.ToString("MMM dd") })
                .ToList();
            ViewData["RecentProposals"] = recentProposals
                .Select(pp => new { pp.Title, pp.Status, DateCreated = pp.DateCreated.ToString("MMM dd, yyyy"), pp.EmployeeName })
                .ToList();
            ViewData["DepartmentNames"] = departmentBreakdown.Select(d => d.Name).ToList();
            ViewData["DepartmentCounts"] = departmentBreakdown.Select(d => d.EmployeeCount).ToList();

            return View();
        }

        /// <summary>
        /// Org-wide task completion trend (all projects, all employees).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CompletedTasksTrend(int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            var counts = await _context.ProjectTasks
                .Where(t => t.Status == "Checked" && t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= startDate)
                .GroupBy(t => new { Year = t.CompletedAt!.Value.Year, Month = t.CompletedAt!.Value.Month, Day = t.CompletedAt!.Value.Day })
                .Select(g => new
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day),
                    Count = g.Count()
                })
                .ToListAsync();

            var labels = Enumerable.Range(0, days).Select(i => startDate.AddDays(i)).ToList();
            var data = labels.Select(d => counts.FirstOrDefault(c => c.Date == d)?.Count ?? 0).ToList();

            return Json(new { labels = labels.Select(d => d.ToString("yyyy-MM-dd")), data });
        }

        /// <summary>
        /// Projects created over time.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProjectsCreatedTrend(int days = 30)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            var counts = await _context.Projects
                .Where(p => p.DateCreated.Date >= startDate)
                .GroupBy(p => new { Year = p.DateCreated.Year, Month = p.DateCreated.Month, Day = p.DateCreated.Day })
                .Select(g => new
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day),
                    Count = g.Count()
                })
                .ToListAsync();

            var labels = Enumerable.Range(0, days).Select(i => startDate.AddDays(i)).ToList();
            var data = labels.Select(d => counts.FirstOrDefault(c => c.Date == d)?.Count ?? 0).ToList();

            return Json(new { labels = labels.Select(d => d.ToString("yyyy-MM-dd")), data });
        }

        /// <summary>
        /// Proposals submitted over time.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProposalsTrend(int days = 30)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            var counts = await _context.ProjectProposals
                .Where(pp => pp.DateCreated.Date >= startDate)
                .GroupBy(pp => new { Year = pp.DateCreated.Year, Month = pp.DateCreated.Month, Day = pp.DateCreated.Day })
                .Select(g => new
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day),
                    Count = g.Count()
                })
                .ToListAsync();

            var labels = Enumerable.Range(0, days).Select(i => startDate.AddDays(i)).ToList();
            var data = labels.Select(d => counts.FirstOrDefault(c => c.Date == d)?.Count ?? 0).ToList();

            return Json(new { labels = labels.Select(d => d.ToString("yyyy-MM-dd")), data });
        }
    }
}
