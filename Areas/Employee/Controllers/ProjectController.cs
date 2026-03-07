using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Services;
using project_lifecycle.ViewModels.ProjectManager;

namespace project_lifecycle.EmployeeArea.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditLogService _audit;
        private readonly INotificationService _notif;

        public ProjectController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAuditLogService audit, INotificationService notif)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
            _notif = notif;
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
                    ProjectManagerName = p.ProjectManager != null ? (p.ProjectManager.FirstName + " " + p.ProjectManager.LastName) : string.Empty,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    IsArchived = p.IsArchived
                })
                .ToListAsync();

            // Mark removed (archived membership) projects
            var membershipStatus = await _context.Members
                .Where(m => m.EmployeeId == employee.Id)
                .Select(m => new { m.ProjectId, m.IsArchived })
                .ToListAsync();

            foreach (var project in projects)
            {
                var membership = membershipStatus.FirstOrDefault(m => m.ProjectId == project.Id);
                if (membership != null && membership.IsArchived)
                    project.IsMemberRemoved = true;
            }

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
                        ProfileImage = m.Employee?.ProfileImage,
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
                        Status = ms.Status,
                        IsArchived = ms.IsArchived
                    })
                    .ToList();
            }

            var vm = new ProjectIndexViewModel { Projects = projects };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Show(int id)
        {
            ViewData["Title"] = "Project Details";

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return Challenge();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.ProjectId == id && m.EmployeeId == employee.Id);
            if (member == null)
            {
                return Forbid();
            }

            var project = await _context.Projects
                .Where(p => p.Id == id)
                .Select(p => new ProjectDetailViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProposalTitle = p.ProjectProposal != null ? p.ProjectProposal.Title : string.Empty,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    IsArchived = p.IsArchived
                })
                .FirstOrDefaultAsync();

            if (project == null) return NotFound();

            // load members and milestones for the show view
            var members = await _context.Members
                .Where(m => m.ProjectId == id)
                .Include(m => m.Employee)
                .Include(m => m.ProjectRole)
                .ToListAsync();

            project.Members = members
                .Select(m => new project_lifecycle.ViewModels.ProjectManager.MemberViewModel
                {
                    Id = m.Id,
                    EmployeeId = m.EmployeeId,
                    EmployeeName = m.Employee != null ? string.Join(" ", new[] { m.Employee.FirstName, m.Employee.MiddleName, m.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : "N/A",
                    ProfileImage = m.Employee?.ProfileImage,
                    ProjectRoleId = m.ProjectRoleId,
                    ProjectRoleName = m.ProjectRole != null ? m.ProjectRole.Name : "N/A"
                })
                .ToList();

            var milestones = await _context.ProjectMilestones
                .Where(pm => pm.ProjectId == id)
                .Include(pm => pm.Milestone)
                .OrderBy(pm => pm.SequenceOrder)
                .ToListAsync();

            project.Milestones = milestones
                .Select(ms => new project_lifecycle.ViewModels.ProjectManager.ProjectMilestoneViewModel
                {
                    Id = ms.Id,
                    MilestoneId = ms.MilestoneId,
                    MilestoneName = ms.Milestone != null ? ms.Milestone.Name : "N/A",
                    SequenceOrder = ms.SequenceOrder,
                    Status = ms.Status
                })
                .ToList();

            // tasks assigned to this employee (via member)
            var taskItems = await _context.TaskMembers
                .Where(tm => tm.MemberId == member.Id)
                .Include(tm => tm.ProjectTask).ThenInclude(pt => pt.ProjectMilestone).ThenInclude(pm => pm.Milestone)
                .Select(tm => new
                {
                    tm.ProjectTask.Id,
                    tm.ProjectTask.Name,
                    tm.ProjectTask.Status,
                    tm.ProjectTask.StartDate,
                    tm.ProjectTask.EndDate,
                    tm.ProjectTask.IsArchived,
                    MilestoneName = tm.ProjectTask.ProjectMilestone != null && tm.ProjectTask.ProjectMilestone.Milestone != null
                        ? tm.ProjectTask.ProjectMilestone.Milestone.Name
                        : string.Empty
                })
                .ToListAsync();

            var vm = new project_lifecycle.ViewModels.Employee.EmployeeProjectViewModel
            {
                Project = project,
                Tasks = taskItems.Select(t => new project_lifecycle.ViewModels.Employee.EmployeeProjectViewModel.TaskItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    Status = t.Status,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    MilestoneName = t.MilestoneName ?? string.Empty,
                    IsArchived = t.IsArchived
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Task(int id)
        {
            ViewData["Title"] = "Task";

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Ensure current user is assigned to this task
            var taskMember = await _context.TaskMembers
                .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                .FirstOrDefaultAsync(tm => tm.ProjectTaskId == id && tm.Member != null && tm.Member.Employee != null && tm.Member.Employee.UserId == userId);

            if (taskMember == null) return Forbid();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pm => pm.Project)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

            // If the parent project is archived, treat the task as archived too
            var taskIsArchived = task.IsArchived || (task.ProjectMilestone?.Project?.IsArchived ?? false);

            var versions = await _context.ProjectTaskVersions
                .Where(v => v.ProjectTaskId == id)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

            var noteVersions = await _context.TaskNoteVersions
                .Where(n => n.ProjectTaskId == id)
                .OrderByDescending(n => n.VersionNumber)
                .ToListAsync();

            ViewBag.ProjectTaskVersions = versions;
            ViewBag.TaskNoteVersions = noteVersions;

            ViewData["TaskId"] = id;
            ViewData["TaskName"] = task.Name;
            ViewData["ProjectId"] = task.ProjectMilestone?.ProjectId;
            ViewData["Instructions"] = task.Instructions ?? string.Empty;
            ViewData["ExistingInput"] = task.Input ?? string.Empty;
            ViewData["TaskStatus"] = task.Status;
            ViewData["TaskNotes"] = task.Notes ?? string.Empty;
            ViewData["IsArchived"] = taskIsArchived;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Task(int id, string Input)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Ensure current user is assigned to this task
            var taskMember = await _context.TaskMembers
                .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                .FirstOrDefaultAsync(tm => tm.ProjectTaskId == id && tm.Member != null && tm.Member.Employee != null && tm.Member.Employee.UserId == userId);

            if (taskMember == null) return Forbid();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pm => pm.Project)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

            if (task.IsArchived || (task.ProjectMilestone?.Project?.IsArchived ?? false))
            {
                TempData["ErrorMessage"] = "This task has been archived and can no longer be edited.";
                return RedirectToAction(nameof(Task), new { id });
            }

            // Save current input as a version before overwriting
            if (!string.IsNullOrWhiteSpace(task.Input))
            {
                var existingVersions = await _context.ProjectTaskVersions
                    .Where(v => v.ProjectTaskId == id)
                    .ToListAsync();
                var nextVersion = (existingVersions.Any() ? existingVersions.Max(v => v.VersionNumber) : 0) + 1;

                _context.ProjectTaskVersions.Add(new project_lifecycle.Models.ProjectTaskVersion
                {
                    ProjectTaskId = id,
                    VersionNumber = nextVersion,
                    Input = task.Input,
                    TaskMemberId = taskMember.Id,
                    DateCreated = DateTime.Now
                });
            }

            task.Input = Input;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Update", "Tasks", $"Submitted input for task '{task.Name}' (ID: {task.Id})", "ProjectTask", task.Id.ToString());

            // Notify the project manager that an employee submitted task input
            var taskWithPM = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pms => pms.Project).ThenInclude(p => p.ProjectManager)
                .FirstOrDefaultAsync(t => t.Id == id);
            var pmUserId = taskWithPM?.ProjectMilestone?.Project?.ProjectManager?.UserId;
            if (!string.IsNullOrEmpty(pmUserId))
            {
                await _notif.CreateAsync(pmUserId,
                    "Task Input Submitted",
                    $"An employee has submitted input for task '{task.Name}'.",
                    "Info", "fas fa-file-circle-check",
                    $"/ProjectManager/Project/Task/{task.Id}",
                    "Task");
            }

            TempData["SuccessMessage"] = "Task input submitted.";
            return RedirectToAction(nameof(Task), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> TaskVersion(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var version = await _context.ProjectTaskVersions
                .Include(v => v.ProjectTask)
                    .ThenInclude(t => t!.ProjectMilestone)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();

            // Verify user is assigned to this task
            var taskMember = await _context.TaskMembers
                .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                .FirstOrDefaultAsync(tm => tm.ProjectTaskId == version.ProjectTaskId && tm.Member != null && tm.Member.Employee != null && tm.Member.Employee.UserId == userId);

            if (taskMember == null) return Forbid();

            return View(version);
        }

        [HttpGet]
        public async Task<IActionResult> TaskNote(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var note = await _context.TaskNoteVersions
                .Include(n => n.ProjectTask)
                    .ThenInclude(t => t!.ProjectMilestone)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null) return NotFound();

            // Verify user is assigned to this task
            var taskMember = await _context.TaskMembers
                .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                .FirstOrDefaultAsync(tm => tm.ProjectTaskId == note.ProjectTaskId && tm.Member != null && tm.Member.Employee != null && tm.Member.Employee.UserId == userId);

            if (taskMember == null) return Forbid();

            return View(note);
        }
    }
}
