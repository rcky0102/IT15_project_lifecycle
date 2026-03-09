using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.ViewModels.ProjectManager;

namespace project_lifecycle.ExecutiveArea.Controllers
{
    [Area("Executive")]
    [Authorize(Roles = "Executive")]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Projects";

            var projects = await _context.Projects
                .OrderByDescending(p => p.DateCreated)
                .Select(p => new ProjectDetailViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProposalTitle = p.ProjectProposal != null ? p.ProjectProposal.Title : string.Empty,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    IsArchived = p.IsArchived
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

            var members = await _context.Members
                .Where(m => m.ProjectId == id)
                .Include(m => m.Employee)
                .Include(m => m.ProjectRole)
                .ToListAsync();

            project.Members = members
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

            var milestones = await _context.ProjectMilestones
                .Where(pm => pm.ProjectId == id)
                .Include(pm => pm.Milestone)
                .OrderBy(pm => pm.SequenceOrder)
                .ToListAsync();

            project.Milestones = milestones
                .Select(ms => new ProjectMilestoneViewModel
                {
                    Id = ms.Id,
                    MilestoneId = ms.MilestoneId,
                    MilestoneName = ms.Milestone != null ? ms.Milestone.Name : "N/A",
                    SequenceOrder = ms.SequenceOrder,
                    Status = ms.Status
                })
                .ToList();

            // All tasks for this project (not filtered by member)
            var taskItems = await _context.ProjectTasks
                .Where(t => t.ProjectMilestone != null && t.ProjectMilestone.ProjectId == id && !t.IsArchived)
                .Include(t => t.ProjectMilestone).ThenInclude(pm => pm.Milestone)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Status,
                    t.StartDate,
                    t.EndDate,
                    MilestoneName = t.ProjectMilestone != null && t.ProjectMilestone.Milestone != null
                        ? t.ProjectMilestone.Milestone.Name
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
                    MilestoneName = t.MilestoneName ?? string.Empty
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Task(int id)
        {
            ViewData["Title"] = "Task";

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

        [HttpGet]
        public async Task<IActionResult> TaskVersion(int id)
        {
            var version = await _context.ProjectTaskVersions
                .Include(v => v.ProjectTask)
                    .ThenInclude(t => t!.ProjectMilestone)
                .Include(v => v.TaskMember).ThenInclude(tm => tm!.Member).ThenInclude(m => m.Employee)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();

            return View(version);
        }

        [HttpGet]
        public async Task<IActionResult> TaskNote(int id)
        {
            var note = await _context.TaskNoteVersions
                .Include(n => n.ProjectTask)
                    .ThenInclude(t => t!.ProjectMilestone)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null) return NotFound();

            return View(note);
        }
    }
}
