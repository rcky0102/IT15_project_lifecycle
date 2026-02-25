using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;

namespace project_lifecycle.ProjectManagerArea.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<ProjectManager?> GetCurrentProjectManagerAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;
            return await _context.ProjectManagers.FirstOrDefaultAsync(pm => pm.UserId == userId);
        }

        public async Task<IActionResult> Index()
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var today = DateTime.Today;
            var in30Days = today.AddDays(30);

            // Projects owned by this PM
            var projectIds = await _context.Projects
                .Where(p => p.ProjectManagerId == pm.Id)
                .Select(p => p.Id)
                .ToListAsync();

            var activeProjects = projectIds.Count;

            // Open (Pending) tasks assigned through this PM's projects
            var openTasks = await _context.ProjectTasks
                .Where(t => t.ProjectManagerId == pm.Id && t.Status == "Pending")
                .CountAsync();

            // Projects whose end date falls in the next 30 days
            var upcomingDeadlines = await _context.Projects
                .Where(p => p.ProjectManagerId == pm.Id && p.EndDate >= today && p.EndDate <= in30Days)
                .CountAsync();

            // Distinct team members across PM's projects
            var teamMembers = await _context.Members
                .Where(m => projectIds.Contains(m.ProjectId))
                .Select(m => m.EmployeeId)
                .Distinct()
                .CountAsync();

            // Task completion rate
            var totalTasks = await _context.ProjectTasks
                .Where(t => t.ProjectManagerId == pm.Id)
                .CountAsync();
            var checkedTasks = await _context.ProjectTasks
                .Where(t => t.ProjectManagerId == pm.Id && t.Status == "Checked")
                .CountAsync();
            var completionPct = totalTasks > 0
                ? (int)Math.Round(checkedTasks * 100.0 / totalTasks)
                : 0;

            ViewData["OpenTasks"]        = openTasks;
            ViewData["ActiveProjects"]   = activeProjects;
            ViewData["UpcomingDeadlines"]= upcomingDeadlines;
            ViewData["TeamMembers"]      = teamMembers;
            ViewData["TasksCompletion"]  = completionPct;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CompletedTasksTrend(int days = 7)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Json(new { labels = Array.Empty<string>(), data = Array.Empty<int>() });

            var cutoff = DateTime.Today.AddDays(-(days - 1));

            var completedTasks = await _context.ProjectTasks
                .Where(t => t.ProjectManagerId == pm.Id
                         && t.Status == "Checked"
                         && t.CompletedAt.HasValue
                         && t.CompletedAt.Value.Date >= cutoff)
                .GroupBy(t => t.CompletedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var labels = new List<string>();
            var data   = new List<int>();

            for (int i = days - 1; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                labels.Add(date.ToString("yyyy-MM-dd"));
                data.Add(completedTasks.FirstOrDefault(x => x.Date == date)?.Count ?? 0);
            }

            return Json(new { labels, data });
        }
    }
}
