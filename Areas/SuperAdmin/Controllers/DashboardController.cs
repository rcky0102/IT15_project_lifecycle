using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Constants;
using project_lifecycle.Data;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // ── Primary stats ──
            var totalUsers = await _userManager.Users.CountAsync();
            var totalEmployees = await _db.Employees.CountAsync();
            var totalProjects = await _db.Projects.CountAsync(p => !p.IsArchived);
            var totalDepts = await _db.Departments.CountAsync(d => !d.IsArchived);

            ViewData["TotalUsers"] = totalUsers;
            ViewData["TotalEmployees"] = totalEmployees;
            ViewData["TotalProjects"] = totalProjects;
            ViewData["TotalDepartments"] = totalDepts;

            // ── Secondary stats ──
            var totalTasks = await _db.ProjectTasks.CountAsync(t => !t.IsArchived);
            var completedTasks = await _db.ProjectTasks.CountAsync(t => !t.IsArchived && t.Status == "Checked");
            var pendingProposals = await _db.ProjectProposals.CountAsync(p => !p.IsArchived && p.Status == "Pending");
            var totalPositions = await _db.Positions.CountAsync(p => !p.IsArchived);

            ViewData["TotalTasks"] = totalTasks;
            ViewData["CompletedTasks"] = completedTasks;
            ViewData["PendingProposals"] = pendingProposals;
            ViewData["TotalPositions"] = totalPositions;

            // ── Task status breakdown ──
            ViewData["TaskChecked"] = completedTasks;
            ViewData["TaskPending"] = await _db.ProjectTasks.CountAsync(t => !t.IsArchived && t.Status == "Pending");
            ViewData["TaskRevision"] = await _db.ProjectTasks.CountAsync(t => !t.IsArchived && t.Status == "Require Revision");

            // ── Proposal status breakdown ──
            ViewData["ProposalApproved"] = await _db.ProjectProposals.CountAsync(p => !p.IsArchived && p.Status == "Approved");
            ViewData["ProposalPending"] = pendingProposals;
            ViewData["ProposalRejected"] = await _db.ProjectProposals.CountAsync(p => !p.IsArchived && p.Status == "Rejected");
            ViewData["ProposalRevision"] = await _db.ProjectProposals.CountAsync(p => !p.IsArchived && p.Status == "Requires Revision");

            // ── Department headcount (active departments only) ──
            var deptData = await _db.Departments
                .Where(d => !d.IsArchived)
                .Select(d => new
                {
                    d.Name,
                    Count = _db.Employees.Count(e => e.DepartmentId == d.Id)
                })
                .OrderByDescending(d => d.Count)
                .Take(10)
                .ToListAsync();

            ViewData["DeptNames"] = deptData.Select(d => d.Name).ToList();
            ViewData["DeptCounts"] = deptData.Select(d => d.Count).ToList();

            // ── Completion rates ──
            var completionRate = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100) : 0;
            ViewData["CompletionRate"] = (int)completionRate;

            var totalProposals = await _db.ProjectProposals.CountAsync(p => !p.IsArchived);
            var approvedProposals = await _db.ProjectProposals.CountAsync(p => !p.IsArchived && p.Status == "Approved");
            var proposalRate = totalProposals > 0 ? Math.Round((double)approvedProposals / totalProposals * 100) : 0;
            ViewData["ProposalRate"] = (int)proposalRate;

            // ── Recent projects ──
            var recentProjects = await _db.Projects
                .OrderByDescending(p => p.DateCreated)
                .Take(5)
                .Select(p => new
                {
                    p.Name,
                    p.IsArchived,
                    p.DateCreated,
                    p.StartDate,
                    p.EndDate
                })
                .ToListAsync();

            ViewData["RecentProjects"] = recentProjects.Cast<dynamic>().ToList();

            // ── Recent proposals ──
            var recentProposals = await _db.ProjectProposals
                .OrderByDescending(p => p.DateCreated)
                .Take(5)
                .Select(p => new
                {
                    p.Title,
                    p.Status,
                    p.DateCreated
                })
                .ToListAsync();

            ViewData["RecentProposals"] = recentProposals.Cast<dynamic>().ToList();

            return View();
        }

        /// <summary>
        /// JSON endpoint: number of user registrations per day.
        /// Uses Identity Users' LockoutEnd as a proxy – falls back to returning zeros.
        /// In practice this would query an audit / registration-date column.
        /// Here we show employee DateHired as a useful proxy for "new people added".
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UserRegistrationTrend(int days = 7)
        {
            var from = DateTime.Today.AddDays(-days);

            var grouped = await _db.AuditLogs
                .Where(a => a.Action == "Create" && a.EntityType == "User"
                         && a.Timestamp >= from)
                .GroupBy(a => a.Timestamp.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<int>();

            for (var d = from; d <= DateTime.Today; d = d.AddDays(1))
            {
                labels.Add(d.ToString("yyyy-MM-dd"));
                data.Add(grouped.FirstOrDefault(g => g.Date == d)?.Count ?? 0);
            }

            return Json(new { labels, data });
        }
    }
}
