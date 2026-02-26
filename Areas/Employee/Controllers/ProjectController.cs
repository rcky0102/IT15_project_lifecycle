using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.ViewModels.ProjectManager;

namespace project_lifecycle.EmployeeArea.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .ToListAsync();

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
                        Status = ms.Status
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
                    EndDate = p.EndDate
                })
                .FirstOrDefaultAsync();

            if (project == null) return NotFound();

            // tasks assigned to this employee (via member)
            var taskItems = await _context.TaskMembers
                .Where(tm => tm.MemberId == member.Id)
                .Include(tm => tm.ProjectTask)
                .Select(tm => new
                {
                    tm.ProjectTask.Id,
                    tm.ProjectTask.Name,
                    tm.ProjectTask.Status,
                    tm.ProjectTask.StartDate,
                    tm.ProjectTask.EndDate
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
                    EndDate = t.EndDate
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
                .Include(t => t.ProjectMilestone)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

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

            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

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
