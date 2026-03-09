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
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Generate a report based on type and date range. Returns JSON for client-side rendering + PDF export.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Generate(string type, DateTime from, DateTime to)
        {
            var fromDate = from.Date;
            var toDate = to.Date.AddDays(1).AddTicks(-1); // include full "to" day

            object report;

            switch (type)
            {
                case "projects":
                    report = await GenerateProjectsReport(fromDate, toDate);
                    break;
                case "tasks":
                    report = await GenerateTasksReport(fromDate, toDate);
                    break;
                case "proposals":
                    report = await GenerateProposalsReport(fromDate, toDate);
                    break;
                case "employees":
                    report = await GenerateEmployeesReport(fromDate, toDate);
                    break;
                case "summary":
                default:
                    report = await GenerateSummaryReport(fromDate, toDate);
                    break;
            }

            return Json(report);
        }

        private async Task<object> GenerateSummaryReport(DateTime from, DateTime to)
        {
            var projectsCreated = await _context.Projects.CountAsync(p => !p.IsArchived && p.DateCreated >= from && p.DateCreated <= to);
            var activeProjects = await _context.Projects.CountAsync(p => !p.IsArchived && p.DateCreated <= to);
            var archivedProjects = await _context.Projects.CountAsync(p => p.IsArchived && p.DateCreated >= from && p.DateCreated <= to);

            var totalTasks = await _context.ProjectTasks.CountAsync(t => !t.IsArchived && t.DateCreated >= from && t.DateCreated <= to);
            var completedTasks = await _context.ProjectTasks.CountAsync(t => !t.IsArchived && t.Status == "Checked" && t.CompletedAt.HasValue && t.CompletedAt.Value >= from && t.CompletedAt.Value <= to);
            var pendingTasks = await _context.ProjectTasks.CountAsync(t => !t.IsArchived && t.Status == "Pending" && t.DateCreated >= from && t.DateCreated <= to);
            var revisionTasks = await _context.ProjectTasks.CountAsync(t => !t.IsArchived && t.Status == "Require Revision" && t.DateCreated >= from && t.DateCreated <= to);

            var totalProposals = await _context.ProjectProposals.CountAsync(pp => !pp.IsArchived && pp.DateCreated >= from && pp.DateCreated <= to);
            var approvedProposals = await _context.ProjectProposals.CountAsync(pp => !pp.IsArchived && pp.Status == "Approved" && pp.DateCreated >= from && pp.DateCreated <= to);
            var pendingProposals = await _context.ProjectProposals.CountAsync(pp => !pp.IsArchived && pp.Status == "Pending" && pp.DateCreated >= from && pp.DateCreated <= to);
            var rejectedProposals = await _context.ProjectProposals.CountAsync(pp => !pp.IsArchived && pp.Status == "Rejected" && pp.DateCreated >= from && pp.DateCreated <= to);

            var newEmployees = await _context.Employees.CountAsync(e => e.DateHired >= from && e.DateHired <= to);
            var totalEmployees = await _context.Employees.CountAsync();
            var totalDepartments = await _context.Departments.CountAsync();

            var milestonesFinished = await _context.ProjectMilestones.CountAsync(pm => pm.Status == "Finished" && pm.DateCreated >= from && pm.DateCreated <= to);
            var totalMilestones = await _context.ProjectMilestones.CountAsync(pm => pm.DateCreated >= from && pm.DateCreated <= to);

            return new
            {
                type = "summary",
                from = from.ToString("MMM dd, yyyy"),
                to = to.ToString("MMM dd, yyyy"),
                data = new
                {
                    projectsCreated,
                    activeProjects,
                    archivedProjects,
                    totalTasks,
                    completedTasks,
                    pendingTasks,
                    revisionTasks,
                    taskCompletionRate = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 1) : 0,
                    totalProposals,
                    approvedProposals,
                    pendingProposals,
                    rejectedProposals,
                    proposalApprovalRate = totalProposals > 0 ? Math.Round((double)approvedProposals / totalProposals * 100, 1) : 0,
                    newEmployees,
                    totalEmployees,
                    totalDepartments,
                    milestonesFinished,
                    totalMilestones,
                    milestoneCompletionRate = totalMilestones > 0 ? Math.Round((double)milestonesFinished / totalMilestones * 100, 1) : 0
                }
            };
        }

        private async Task<object> GenerateProjectsReport(DateTime from, DateTime to)
        {
            var projects = await _context.Projects
                .Include(p => p.ProjectManager)
                .Where(p => !p.IsArchived && p.DateCreated >= from && p.DateCreated <= to)
                .OrderByDescending(p => p.DateCreated)
                .Select(p => new
                {
                    p.Name,
                    Manager = p.ProjectManager != null ? p.ProjectManager.FirstName + " " + p.ProjectManager.LastName : "Unassigned",
                    StartDate = p.StartDate.ToString("MMM dd, yyyy"),
                    EndDate = p.EndDate.ToString("MMM dd, yyyy"),
                    DateCreated = p.DateCreated.ToString("MMM dd, yyyy"),
                    Status = p.IsArchived ? "Archived" : "Active",
                    MemberCount = _context.Members.Count(m => m.ProjectId == p.Id),
                    TaskCount = _context.ProjectTasks.Count(t => !t.IsArchived && t.ProjectMilestone != null && t.ProjectMilestone.ProjectId == p.Id),
                    CompletedTaskCount = _context.ProjectTasks.Count(t => !t.IsArchived && t.ProjectMilestone != null && t.ProjectMilestone.ProjectId == p.Id && t.Status == "Checked")
                })
                .ToListAsync();

            return new
            {
                type = "projects",
                from = from.ToString("MMM dd, yyyy"),
                to = to.ToString("MMM dd, yyyy"),
                totalCount = projects.Count,
                rows = projects
            };
        }

        private async Task<object> GenerateTasksReport(DateTime from, DateTime to)
        {
            var tasks = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pm => pm!.Project)
                .Include(t => t.ProjectMilestone).ThenInclude(pm => pm!.Milestone)
                .Where(t => !t.IsArchived && t.DateCreated >= from && t.DateCreated <= to)
                .OrderByDescending(t => t.DateCreated)
                .Select(t => new
                {
                    t.Name,
                    Project = t.ProjectMilestone != null && t.ProjectMilestone.Project != null ? t.ProjectMilestone.Project.Name : "—",
                    Milestone = t.ProjectMilestone != null && t.ProjectMilestone.Milestone != null ? t.ProjectMilestone.Milestone.Name : "—",
                    t.Status,
                    StartDate = t.StartDate.ToString("MMM dd, yyyy"),
                    EndDate = t.EndDate.ToString("MMM dd, yyyy"),
                    CompletedAt = t.CompletedAt.HasValue ? t.CompletedAt.Value.ToString("MMM dd, yyyy") : "—",
                    DateCreated = t.DateCreated.ToString("MMM dd, yyyy"),
                    AssigneeCount = _context.TaskMembers.Count(tm => tm.ProjectTaskId == t.Id)
                })
                .ToListAsync();

            var completed = tasks.Count(t => t.Status == "Checked");
            var pending = tasks.Count(t => t.Status == "Pending");
            var revision = tasks.Count(t => t.Status == "Require Revision");

            return new
            {
                type = "tasks",
                from = from.ToString("MMM dd, yyyy"),
                to = to.ToString("MMM dd, yyyy"),
                totalCount = tasks.Count,
                completed,
                pending,
                revision,
                rows = tasks
            };
        }

        private async Task<object> GenerateProposalsReport(DateTime from, DateTime to)
        {
            var proposals = await _context.ProjectProposals
                .Include(pp => pp.Employee).ThenInclude(e => e!.Department)
                .Where(pp => !pp.IsArchived && pp.DateCreated >= from && pp.DateCreated <= to)
                .OrderByDescending(pp => pp.DateCreated)
                .Select(pp => new
                {
                    pp.Title,
                    pp.Status,
                    Employee = pp.Employee != null ? pp.Employee.FirstName + " " + pp.Employee.LastName : "Unknown",
                    Department = pp.Employee != null && pp.Employee.Department != null ? pp.Employee.Department.Name : "—",
                    DateCreated = pp.DateCreated.ToString("MMM dd, yyyy"),
                    pp.IsArchived
                })
                .ToListAsync();

            var approved = proposals.Count(p => p.Status == "Approved");
            var pending = proposals.Count(p => p.Status == "Pending");
            var rejected = proposals.Count(p => p.Status == "Rejected");

            return new
            {
                type = "proposals",
                from = from.ToString("MMM dd, yyyy"),
                to = to.ToString("MMM dd, yyyy"),
                totalCount = proposals.Count,
                approved,
                pending,
                rejected,
                rows = proposals
            };
        }

        private async Task<object> GenerateEmployeesReport(DateTime from, DateTime to)
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.DateHired >= from && e.DateHired <= to)
                .OrderByDescending(e => e.DateHired)
                .Select(e => new
                {
                    Name = e.FirstName + " " + e.LastName,
                    e.EmployeeNumber,
                    Department = e.Department != null ? e.Department.Name : "—",
                    Position = e.Position != null ? e.Position.Name : "—",
                    DateHired = e.DateHired.ToString("MMM dd, yyyy"),
                    ProjectCount = _context.Members.Count(m => m.EmployeeId == e.Id),
                    TaskCount = _context.TaskMembers.Count(tm => tm.Member != null && tm.Member.EmployeeId == e.Id)
                })
                .ToListAsync();

            // Department distribution
            var deptDistribution = employees
                .GroupBy(e => e.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            return new
            {
                type = "employees",
                from = from.ToString("MMM dd, yyyy"),
                to = to.ToString("MMM dd, yyyy"),
                totalCount = employees.Count,
                deptDistribution,
                rows = employees
            };
        }
    }
}
