using System;
using System.Collections.Generic;
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

            // Projects owned by this PM (non-archived)
            var projectIds = await _context.Projects
                .Where(p => p.ProjectManagerId == pm.Id && !p.IsArchived)
                .Select(p => p.Id)
                .ToListAsync();

            var activeProjects = projectIds.Count;

            // All tasks under this PM (non-archived)
            var allTasks = await _context.ProjectTasks
                .Where(t => t.ProjectManagerId == pm.Id && !t.IsArchived)
                .Select(t => new { t.Id, t.Status, t.EndDate, t.CompletedAt, t.Name, t.ProjectMilestoneId })
                .ToListAsync();

            var totalTasks = allTasks.Count;
            var checkedTasks = allTasks.Count(t => t.Status == "Checked");
            var pendingTasks = allTasks.Count(t => t.Status == "Pending");
            var revisionTasks = allTasks.Count(t => t.Status == "Require Revision");
            var overdueTasks = allTasks.Count(t => t.Status != "Checked" && t.EndDate < today);

            var completionPct = totalTasks > 0
                ? (int)Math.Round(checkedTasks * 100.0 / totalTasks)
                : 0;

            // Projects whose end date falls in the next 30 days
            var upcomingDeadlines = await _context.Projects
                .Where(p => p.ProjectManagerId == pm.Id && !p.IsArchived && p.EndDate >= today && p.EndDate <= in30Days)
                .CountAsync();

            // Distinct team members across PM's projects
            var teamMembers = await _context.Members
                .Where(m => projectIds.Contains(m.ProjectId))
                .Select(m => m.EmployeeId)
                .Distinct()
                .CountAsync();

            // ?? Milestone stats ??
            var milestoneIds = await _context.ProjectMilestones
                .Where(ms => projectIds.Contains(ms.ProjectId) && !ms.IsArchived)
                .Select(ms => new { ms.Id, ms.Status })
                .ToListAsync();

            var totalMilestones = milestoneIds.Count;
            var finishedMilestones = milestoneIds.Count(m => m.Status == "Finished");
            var milestonePct = totalMilestones > 0
                ? (int)Math.Round(finishedMilestones * 100.0 / totalMilestones)
                : 0;

            // ?? Due projects percentage (for workload bar) ??
            var duePct = activeProjects > 0
                ? (int)Math.Round(upcomingDeadlines * 100.0 / activeProjects)
                : 0;

            // ?? Open tasks percentage ??
            var openTasksPct = totalTasks > 0
                ? (int)Math.Round(pendingTasks * 100.0 / totalTasks)
                : 0;

            // ?? Task status counts for doughnut chart ??
            ViewData["CheckedTasks"] = checkedTasks;
            ViewData["PendingTasks"] = pendingTasks;
            ViewData["RevisionTasks"] = revisionTasks;

            // ?? Overdue tasks ??
            ViewData["OverdueTasks"] = overdueTasks;

            // ?? Stat cards ??
            ViewData["OpenTasks"] = pendingTasks;
            ViewData["ActiveProjects"] = activeProjects;
            ViewData["UpcomingDeadlines"] = upcomingDeadlines;
            ViewData["TeamMembers"] = teamMembers;
            ViewData["TasksCompletion"] = completionPct;
            ViewData["TotalTasks"] = totalTasks;

            // ?? Milestone stats ??
            ViewData["TotalMilestones"] = totalMilestones;
            ViewData["FinishedMilestones"] = finishedMilestones;
            ViewData["MilestonePct"] = milestonePct;

            // ?? Workload percentages ??
            ViewData["DuePct"] = duePct;
            ViewData["OpenTasksPct"] = openTasksPct;

            // ?? Recent activity from audit logs ??
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var recentActivity = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(8)
                .Select(a => new { a.Action, a.Module, a.Description, a.Timestamp })
                .ToListAsync();

            ViewData["RecentActivity"] = recentActivity
                .Select(a => new
                {
                    a.Action,
                    a.Module,
                    a.Description,
                    Time = FormatRelativeTime(a.Timestamp)
                })
                .ToList();

            // ?? Upcoming deadline projects ??
            var deadlineProjects = await _context.Projects
                .Where(p => p.ProjectManagerId == pm.Id && !p.IsArchived && p.EndDate >= today && p.EndDate <= in30Days)
                .OrderBy(p => p.EndDate)
                .Take(5)
                .Select(p => new { p.Id, p.Name, p.EndDate })
                .ToListAsync();

            ViewData["DeadlineProjects"] = deadlineProjects
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    EndDate = p.EndDate.ToString("MMM dd, yyyy"),
                    DaysLeft = (int)(p.EndDate - today).TotalDays
                })
                .ToList();

            // ?? Overdue tasks list ??
            var overdueTasksList = allTasks
                .Where(t => t.Status != "Checked" && t.EndDate < today)
                .OrderBy(t => t.EndDate)
                .Take(5)
                .Select(t => new { t.Name, EndDate = t.EndDate.ToString("MMM dd, yyyy") })
                .ToList();

            ViewData["OverdueTasksList"] = overdueTasksList;

            return View();
        }

        private static string FormatRelativeTime(DateTime timestamp)
        {
            var now = DateTime.UtcNow;
            var diff = now - timestamp;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 2) return "Yesterday";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return timestamp.ToString("MMM dd, yyyy");
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
