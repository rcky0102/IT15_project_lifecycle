using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;

namespace project_lifecycle.HumanResourceArea.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "HumanResource")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var now           = DateTime.Now;
            var firstOfMonth  = new DateTime(now.Year, now.Month, 1);
            var quarterStart  = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);

            ViewData["TotalEmployees"]      = await _db.Employees.CountAsync();
            ViewData["NewHiresThisMonth"]   = await _db.Employees.CountAsync(e => e.DateHired >= firstOfMonth);
            ViewData["NewHiresThisQuarter"] = await _db.Employees.CountAsync(e => e.DateHired >= quarterStart);
            ViewData["TotalDepartments"]    = await _db.Departments.CountAsync(d => !d.IsArchived);
            ViewData["TotalPositions"]      = await _db.Positions.CountAsync(p => !p.IsArchived);

            // Department distribution (top 8 by employee count)
            var deptData = await _db.Employees
                .Include(e => e.Department)
                .Where(e => e.Department != null && !e.Department.IsArchived)
                .GroupBy(e => e.Department!.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(d => d.Count)
                .Take(8)
                .ToListAsync();

            ViewData["DeptNames"]  = deptData.Select(d => d.Name).ToList();
            ViewData["DeptCounts"] = deptData.Select(d => d.Count).ToList();

            // Last 10 HR audit entries
            var recentActivity = await _db.AuditLogs
                .Where(a => a.Role == "HumanResource")
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .Select(a => new { a.Action, a.Module, a.Description, a.Timestamp })
                .ToListAsync();

            ViewBag.RecentActivity = recentActivity.Cast<object>().ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> HeadcountTrend(int months = 6)
        {
            if (months < 1 || months > 24) months = 6;
            var now = DateTime.Now;
            var labels = new List<string>();
            var data   = new List<int>();

            for (int i = months - 1; i >= 0; i--)
            {
                var target     = now.AddMonths(-i);
                var endOfMonth = new DateTime(target.Year, target.Month,
                                             DateTime.DaysInMonth(target.Year, target.Month));
                labels.Add(target.ToString("MMM yyyy"));
                data.Add(await _db.Employees.CountAsync(e => e.DateHired <= endOfMonth));
            }

            return Json(new { labels, data });
        }
    }
}
