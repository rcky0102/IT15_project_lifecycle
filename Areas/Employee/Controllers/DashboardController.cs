using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using project_lifecycle.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

        /// <summary>Helper – returns the employee ID for the current user, or null.</summary>
        private async Task<(string? userId, int? employeeId)> ResolveEmployee()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return (null, null);
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            return (userId, emp?.Id);
        }

        /// <summary>Returns all distinct ProjectTask IDs assigned to the given employee.</summary>
        private IQueryable<int> EmployeeTaskIds(int employeeId)
        {
            return _context.TaskMembers
                .Where(tm => tm.Member != null && tm.Member.EmployeeId == employeeId)
                .Select(tm => tm.ProjectTaskId)
                .Distinct();
        }

        public async Task<IActionResult> Index()
        {
            int activeProjects = 0;
            int approvedProposals = 0;
            int completedTasks = 0;
            int needsRevisionTasks = 0;

            // Task-status breakdown for doughnut chart
            int statusPending = 0;
            int statusChecked = 0;
            int statusRevision = 0;

            // Upcoming deadlines (tasks due within 7 days that are not Checked)
            var upcomingDeadlines = new List<object>();

            // Workload stats
            int totalAssignedTasks = 0;
            double completionPct = 0;
            int documentsOwned = 0;
            int unreadNotifications = 0;

            // Recent activity from notifications
            var recentActivity = new List<object>();

            var (userId, employeeId) = await ResolveEmployee();

            if (employeeId.HasValue)
            {
                var empId = employeeId.Value;

                // Approved Proposals created by this employee
                approvedProposals = await _context.ProjectProposals
                    .CountAsync(pp => pp.EmployeeId == empId && pp.Status == "Approved" && !pp.IsArchived);

                var taskIds = EmployeeTaskIds(empId);

                // Projects where this employee is a member (non-archived)
                activeProjects = await _context.Members
                    .Where(m => m.EmployeeId == empId)
                    .Select(m => m.ProjectId)
                    .Distinct()
                    .CountAsync(pid => _context.Projects.Any(p => p.Id == pid && !p.IsArchived));

                // Task counts by status
                var myTasks = _context.ProjectTasks
                    .Where(t => taskIds.Contains(t.Id));

                totalAssignedTasks = await myTasks.CountAsync();
                statusChecked = await myTasks.CountAsync(t => t.Status == "Checked");
                statusPending = await myTasks.CountAsync(t => t.Status == "Pending");
                statusRevision = await myTasks.CountAsync(t => t.Status == "Require Revision");

                completedTasks = statusChecked;
                needsRevisionTasks = statusRevision;

                completionPct = totalAssignedTasks > 0
                    ? Math.Round((double)statusChecked / totalAssignedTasks * 100, 0)
                    : 0;

                // Upcoming deadlines – tasks ending in next 7 days that are still open
                var now = DateTime.Now;
                var sevenDaysLater = now.AddDays(7);
                upcomingDeadlines = await myTasks
                    .Where(t => t.Status != "Checked" && t.EndDate >= now && t.EndDate <= sevenDaysLater)
                    .OrderBy(t => t.EndDate)
                    .Take(5)
                    .Select(t => new { t.Id, t.Name, t.EndDate, t.Status } as object)
                    .ToListAsync();

                // Documents where this employee is owner or collaborator
                documentsOwned = await _context.Documents
                    .CountAsync(d => !d.IsArchived &&
                        (d.OwnerEmployeeId == empId ||
                         _context.DocumentCollaborators.Any(dc => dc.DocumentId == d.Id && dc.EmployeeId == empId)));
            }

            if (!string.IsNullOrEmpty(userId))
            {
                // Unread notifications
                unreadNotifications = await _context.Notifications
                    .CountAsync(n => n.RecipientId == userId && !n.IsRead);

                // Recent activity – latest 8 notifications for this user
                recentActivity = await _context.Notifications
                    .Where(n => n.RecipientId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(8)
                    .Select(n => new
                    {
                        n.Title,
                        n.Message,
                        n.Type,
                        Icon = n.Icon ?? "fas fa-bell",
                        n.Link,
                        n.Module,
                        n.CreatedAt,
                        n.IsRead
                    } as object)
                    .ToListAsync();
            }

            ViewData["ActiveProjects"] = activeProjects;
            ViewData["ApprovedProposals"] = approvedProposals;
            ViewData["CompletedTasks"] = completedTasks;
            ViewData["NeedsRevision"] = needsRevisionTasks;

            // Doughnut chart data
            ViewData["StatusChecked"] = statusChecked;
            ViewData["StatusPending"] = statusPending;
            ViewData["StatusRevision"] = statusRevision;

            // Workload
            ViewData["TotalAssignedTasks"] = totalAssignedTasks;
            ViewData["CompletionPct"] = completionPct;
            ViewData["DocumentsOwned"] = documentsOwned;
            ViewData["UnreadNotifications"] = unreadNotifications;

            // Complex objects via ViewBag
            ViewBag.UpcomingDeadlines = upcomingDeadlines;
            ViewBag.RecentActivity = recentActivity;

            return View();
        }

        /// <summary>Returns completed-tasks-per-day trend scoped to the current employee.</summary>
        [HttpGet]
        public async Task<IActionResult> CompletedTasksTrend(int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            var (_, employeeId) = await ResolveEmployee();

            IQueryable<Models.ProjectTask> query = _context.ProjectTasks
                .Where(t => t.Status == "Checked" && t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= startDate);

            // Scope to employee's tasks when possible
            if (employeeId.HasValue)
            {
                var taskIds = EmployeeTaskIds(employeeId.Value);
                query = query.Where(t => taskIds.Contains(t.Id));
            }

            var counts = await query
                .GroupBy(t => new { Year = t.CompletedAt!.Value.Year, Month = t.CompletedAt.Value.Month, Day = t.CompletedAt.Value.Day })
                .Select(g => new
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day),
                    Count = g.Count()
                })
                .ToListAsync();

            var labels = Enumerable.Range(0, days)
                .Select(i => startDate.AddDays(i))
                .ToList();

            var data = labels.Select(d => counts.FirstOrDefault(c => c.Date == d)?.Count ?? 0).ToList();

            return Json(new { labels = labels.Select(d => d.ToString("yyyy-MM-dd")), data });
        }

        /// <summary>Returns task-status breakdown scoped to the current employee (for AJAX refresh).</summary>
        [HttpGet]
        public async Task<IActionResult> TaskStatusBreakdown()
        {
            var (_, employeeId) = await ResolveEmployee();
            int checkedCount = 0, pendingCount = 0, revisionCount = 0;

            if (employeeId.HasValue)
            {
                var taskIds = EmployeeTaskIds(employeeId.Value);
                var myTasks = _context.ProjectTasks.Where(t => taskIds.Contains(t.Id));
                checkedCount = await myTasks.CountAsync(t => t.Status == "Checked");
                pendingCount = await myTasks.CountAsync(t => t.Status == "Pending");
                revisionCount = await myTasks.CountAsync(t => t.Status == "Require Revision");
            }

            return Json(new
            {
                labels = new[] { "Completed", "Pending", "Require Revision" },
                data = new[] { checkedCount, pendingCount, revisionCount }
            });
        }
    }
}
