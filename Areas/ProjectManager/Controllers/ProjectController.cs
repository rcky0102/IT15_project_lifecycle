using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;
using project_lifecycle.ViewModels.ProjectManager;

namespace project_lifecycle.ProjectManagerArea.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<ProjectController> _logger;
        private readonly IAuditLogService _audit;
        private readonly INotificationService _notif;
        private readonly INagerHolidayService _holidayService;

        public ProjectController(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<ProjectController> logger, IAuditLogService audit, INotificationService notif, INagerHolidayService holidayService)
        {
            _context = context;
            _logger = logger;
            _audit = audit;
            _notif = notif;
            _holidayService = holidayService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveTask(int projectTaskId)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pmst => pmst.Project)
                .FirstOrDefaultAsync(t => t.Id == projectTaskId);

            if (task == null) return NotFound();

            if (task.ProjectMilestone == null || task.ProjectMilestone.Project == null || task.ProjectMilestone.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            // Soft-archive the task instead of deleting related data
            task.IsArchived = true;
            await _context.SaveChangesAsync();

            // After archiving, recompute milestone status based only on active (non-archived) tasks
            var pmstToUpdate = await _context.ProjectMilestones.FirstOrDefaultAsync(p => p.Id == task.ProjectMilestoneId);
            if (pmstToUpdate != null)
            {
                var activeTasks = await _context.ProjectTasks
                    .Where(t => t.ProjectMilestoneId == pmstToUpdate.Id && !t.IsArchived)
                    .ToListAsync();

                if (activeTasks.Count > 0)
                {
                    var allChecked = activeTasks.All(t => string.Equals(t.Status, "Checked", StringComparison.OrdinalIgnoreCase));
                    if (allChecked && !string.Equals(pmstToUpdate.Status, "Finished", StringComparison.OrdinalIgnoreCase))
                    {
                        pmstToUpdate.Status = "Finished";
                        _context.ProjectMilestones.Update(pmstToUpdate);
                        await _context.SaveChangesAsync();
                    }
                    else if (!allChecked && string.Equals(pmstToUpdate.Status, "Finished", StringComparison.OrdinalIgnoreCase))
                    {
                        pmstToUpdate.Status = "Unfinished";
                        _context.ProjectMilestones.Update(pmstToUpdate);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // No active tasks -> milestone should be Unfinished
                    if (string.Equals(pmstToUpdate.Status, "Finished", StringComparison.OrdinalIgnoreCase))
                    {
                        pmstToUpdate.Status = "Unfinished";
                        _context.ProjectMilestones.Update(pmstToUpdate);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // Sync parent project status
            if (pmstToUpdate?.Project != null)
                await SyncProjectStatusAsync(pmstToUpdate.ProjectId);
            else if (task.ProjectMilestone?.ProjectId is int archiveProjId)
                await SyncProjectStatusAsync(archiveProjId);

            await _audit.LogAsync(User, "Archive", "Tasks", $"Archived task '{task.Name}' (ID: {task.Id})", "ProjectTask", task.Id.ToString());

            TempData["SuccessMessage"] = "Task archived.";
            return RedirectToAction(nameof(Milestone), new { projectMilestoneId = task.ProjectMilestoneId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveTask(int projectTaskId)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pmst => pmst.Project)
                .FirstOrDefaultAsync(t => t.Id == projectTaskId);

            if (task == null) return NotFound();

            if (task.ProjectMilestone == null || task.ProjectMilestone.Project == null || task.ProjectMilestone.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            task.IsArchived = false;
            await _context.SaveChangesAsync();

            // After unarchiving, recompute milestone status based only on active (non-archived) tasks
            var pmstToUpdate = await _context.ProjectMilestones.FirstOrDefaultAsync(p => p.Id == task.ProjectMilestoneId);
            if (pmstToUpdate != null)
            {
                var activeTasks = await _context.ProjectTasks
                    .Where(t => t.ProjectMilestoneId == pmstToUpdate.Id && !t.IsArchived)
                    .ToListAsync();

                if (activeTasks.Count > 0)
                {
                    var allChecked = activeTasks.All(t => string.Equals(t.Status, "Checked", StringComparison.OrdinalIgnoreCase));
                    if (allChecked && !string.Equals(pmstToUpdate.Status, "Finished", StringComparison.OrdinalIgnoreCase))
                    {
                        pmstToUpdate.Status = "Finished";
                        _context.ProjectMilestones.Update(pmstToUpdate);
                        await _context.SaveChangesAsync();
                    }
                    else if (!allChecked && string.Equals(pmstToUpdate.Status, "Finished", StringComparison.OrdinalIgnoreCase))
                    {
                        pmstToUpdate.Status = "Unfinished";
                        _context.ProjectMilestones.Update(pmstToUpdate);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // No active tasks -> milestone should be Unfinished
                    if (string.Equals(pmstToUpdate.Status, "Finished", StringComparison.OrdinalIgnoreCase))
                    {
                        pmstToUpdate.Status = "Unfinished";
                        _context.ProjectMilestones.Update(pmstToUpdate);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // Sync parent project status
            if (pmstToUpdate?.Project != null)
                await SyncProjectStatusAsync(pmstToUpdate.ProjectId);
            else if (task.ProjectMilestone?.ProjectId is int unarchiveProjId)
                await SyncProjectStatusAsync(unarchiveProjId);

            await _audit.LogAsync(User, "Unarchive", "Tasks", $"Unarchived task '{task.Name}' (ID: {task.Id})", "ProjectTask", task.Id.ToString());

            TempData["SuccessMessage"] = "Task unarchived.";
            return RedirectToAction(nameof(Milestone), new { projectMilestoneId = task.ProjectMilestoneId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMilestoneStatus(int projectMilestoneId, string newStatus)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var pmst = await _context.ProjectMilestones.Include(p => p.Project).FirstOrDefaultAsync(p => p.Id == projectMilestoneId);
            if (pmst == null) return NotFound();

            if (pmst.Project == null || pmst.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            newStatus = (newStatus ?? string.Empty).Trim();
            if (!string.Equals(newStatus, "Finished", StringComparison.OrdinalIgnoreCase) && !string.Equals(newStatus, "Unfinished", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Invalid status.";
                return RedirectToAction(nameof(Milestone), new { projectMilestoneId });
            }

            pmst.Status = string.Equals(newStatus, "Finished", StringComparison.OrdinalIgnoreCase) ? "Finished" : "Unfinished";
            await _context.SaveChangesAsync();

            // Sync parent project status
            await SyncProjectStatusAsync(pmst.ProjectId);

            TempData["SuccessMessage"] = "Milestone status updated.";
            await _audit.LogAsync(User, "Update", "Project Milestones", $"Updated milestone status (ID: {pmst.Id}) to {pmst.Status}", "ProjectMilestone", pmst.Id.ToString());

            return RedirectToAction(nameof(Milestone), new { projectMilestoneId = pmst.Id });
        }

        private async Task<ProjectManager?> GetCurrentProjectManagerAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;
            return await _context.ProjectManagers.FirstOrDefaultAsync(pm => pm.UserId == userId);
        }

        /// <summary>
        /// Syncs the project's Status to "Finished" when every non-archived milestone
        /// is "Finished", and back to "Unfinished" otherwise.
        /// </summary>
        private async System.Threading.Tasks.Task SyncProjectStatusAsync(int projectId)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return;

            var milestones = await _context.ProjectMilestones
                .Where(m => m.ProjectId == projectId && !m.IsArchived)
                .ToListAsync();

            var allFinished = milestones.Count > 0 &&
                              milestones.All(m => string.Equals(m.Status, "Finished", StringComparison.OrdinalIgnoreCase));

            var newStatus = allFinished ? "Finished" : "Unfinished";
            if (!string.Equals(project.Status, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                project.Status = newStatus;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IActionResult> Index()
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var projects = await _context.Projects
                .Where(p => p.ProjectManagerId == pm.Id)
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
                .Where(pmst => projectIds.Contains(pmst.ProjectId))
                .Include(pmst => pmst.Milestone)
                .OrderBy(pmst => pmst.SequenceOrder)
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
                        ProfileImage = m.Employee != null ? m.Employee.ProfileImage : null,
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

            var employeeRows = await _context.Employees
                .Where(e => e.DepartmentId == pm.DepartmentId)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Select(e => new { e.Id, e.FirstName, e.MiddleName, e.LastName })
                .ToListAsync();

            var availableEmployees = employeeRows
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = string.Join(" ", new[] { e.FirstName, e.MiddleName, e.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                })
                .ToList();

            var roles = await _context.ProjectRoles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();

            var milestoneTemplates = await _context.Milestones
                .OrderBy(m => m.Name)
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name })
                .ToListAsync();

            var vm = new ProjectIndexViewModel
            {
                Projects = projects,
                AvailableEmployees = availableEmployees,
                AvailableProjectRoles = roles,
                AvailableMilestones = milestoneTemplates
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var project = await _context.Projects
                .Where(p => p.Id == id && p.ProjectManagerId == pm.Id)
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

            var projectIds = new[] { project.Id };

            var members = await _context.Members
                .Where(m => projectIds.Contains(m.ProjectId))
                .Include(m => m.Employee)
                .Include(m => m.ProjectRole)
                .ToListAsync();

            var milestones = await _context.ProjectMilestones
                .Where(pmst => projectIds.Contains(pmst.ProjectId))
                .Include(pmst => pmst.Milestone)
                .OrderBy(pmst => pmst.SequenceOrder)
                .ToListAsync();

            project.Members = members
                .Where(m => m.ProjectId == project.Id)
                .Select(m => new MemberViewModel
                {
                    Id = m.Id,
                    EmployeeId = m.EmployeeId,
                    EmployeeName = m.Employee != null ? string.Join(" ", new[] { m.Employee.FirstName, m.Employee.MiddleName, m.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : "N/A",
                    ProfileImage = m.Employee != null ? m.Employee.ProfileImage : null,
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

            var employeeRows = await _context.Employees
                .Where(e => e.DepartmentId == pm.DepartmentId)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Select(e => new { e.Id, e.FirstName, e.MiddleName, e.LastName })
                .ToListAsync();

            var availableEmployees = employeeRows
                .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = string.Join(" ", new[] { e.FirstName, e.MiddleName, e.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                })
                .ToList();

            var roles = await _context.ProjectRoles
                .OrderBy(r => r.Name)
                .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();

            var milestoneTemplates = await _context.Milestones
                .OrderBy(m => m.Name)
                .Select(m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = m.Id.ToString(), Text = m.Name })
                .ToListAsync();

            var vm = new project_lifecycle.ViewModels.ProjectManager.ProjectManageViewModel
            {
                Project = project,
                AvailableEmployees = availableEmployees,
                AvailableProjectRoles = roles,
                AvailableMilestones = milestoneTemplates
            };

            return View("Details", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int id, int? milestoneId = null)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var project = await _context.Projects
                .Where(p => p.Id == id && p.ProjectManagerId == pm.Id)
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
                .Where(m => m.ProjectId == project.Id)
                .Include(m => m.Employee)
                .Include(m => m.ProjectRole)
                .ToListAsync();

            var milestones = await _context.ProjectMilestones
                .Where(pmst => pmst.ProjectId == project.Id)
                .Include(pmst => pmst.Milestone)
                .OrderBy(pmst => pmst.SequenceOrder)
                .ToListAsync();

            project.Members = members
                .Select(m => new MemberViewModel
                {
                    Id = m.Id,
                    EmployeeId = m.EmployeeId,
                    EmployeeName = m.Employee != null ? string.Join(" ", new[] { m.Employee.FirstName, m.Employee.MiddleName, m.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : "N/A",
                    ProfileImage = m.Employee != null ? m.Employee.ProfileImage : null,
                    ProjectRoleId = m.ProjectRoleId,
                    ProjectRoleName = m.ProjectRole != null ? m.ProjectRole.Name : "N/A"
                })
                .ToList();

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

            var employeeRows = await _context.Employees
                .Where(e => e.DepartmentId == pm.DepartmentId)
                .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                .Select(e => new { e.Id, e.FirstName, e.MiddleName, e.LastName })
                .ToListAsync();

            var vm = new project_lifecycle.ViewModels.ProjectManager.ProjectManageViewModel
            {
                Project = project,
                AvailableEmployees = employeeRows
                    .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = string.Join(" ", new[] { e.FirstName, e.MiddleName, e.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                    })
                    .ToList(),
                AvailableProjectRoles = await _context.ProjectRoles
                    .OrderBy(r => r.Name)
                    .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                    .ToListAsync(),
                AvailableMilestones = await _context.Milestones
                    .OrderBy(m => m.Name)
                    .Select(m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = m.Id.ToString(), Text = m.Name })
                    .ToListAsync()
            };

            if (milestoneId.HasValue)
                ViewData["PreselectedMilestoneId"] = milestoneId.Value;

            return View("Create", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Milestone(int projectMilestoneId)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var pmst = await _context.ProjectMilestones
                .Include(p => p.Project)
                .Include(p => p.Milestone)
                .FirstOrDefaultAsync(p => p.Id == projectMilestoneId && p.Project != null && p.Project.ProjectManagerId == pm.Id);

            if (pmst == null) return NotFound();

            var projectId = pmst.ProjectId;

            var tasks = await _context.ProjectTasks
                .Where(t => t.ProjectMilestoneId == pmst.Id)
                .ToListAsync();

            // Do not filter out archived tasks here; the view will let the user filter (Active/Archived/All)

            var taskMembers = await _context.TaskMembers
                .Where(tm => tasks.Select(t => t.Id).Contains(tm.ProjectTaskId))
                .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                .ToListAsync();

            var members = await _context.Members
                .Where(m => m.ProjectId == projectId)
                .Include(m => m.Employee)
                .ToListAsync();

            var vm = new project_lifecycle.ViewModels.ProjectManager.MilestoneViewModel
            {
                ProjectId = pmst.ProjectId,
                ProjectName = pmst.Project != null ? pmst.Project.Name : string.Empty,
                ProjectMilestoneId = pmst.Id,
                MilestoneId = pmst.MilestoneId,
                MilestoneName = pmst.Milestone != null ? pmst.Milestone.Name : string.Empty,
                SequenceOrder = pmst.SequenceOrder,
                Status = pmst.Status
            };

            vm.Tasks = tasks.Select(t => new project_lifecycle.ViewModels.ProjectManager.ProjectTaskItemViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.Status,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                AssignedMemberName = taskMembers.FirstOrDefault(tm => tm.ProjectTaskId == t.Id)?.Member?.Employee != null ? string.Join(" ", new[] { taskMembers.FirstOrDefault(tm => tm.ProjectTaskId == t.Id)!.Member!.Employee!.FirstName, taskMembers.FirstOrDefault(tm => tm.ProjectTaskId == t.Id)!.Member!.Employee!.MiddleName, taskMembers.FirstOrDefault(tm => tm.ProjectTaskId == t.Id)!.Member!.Employee!.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : null,
                IsArchived = t.IsArchived
            }).ToList();

            vm.AvailableMembers = members.Select(m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.Employee != null ? string.Join(" ", new[] { m.Employee.FirstName, m.Employee.MiddleName, m.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : "N/A"
            }).ToList();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Task(int id)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pmst => pmst.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            if (task.ProjectMilestone == null || task.ProjectMilestone.Project == null || task.ProjectMilestone.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            var taskMember = await _context.TaskMembers
                .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                .FirstOrDefaultAsync(tm => tm.ProjectTaskId == id);

            var assignedName = taskMember?.Member?.Employee != null ? string.Join(" ", new[] { taskMember.Member.Employee.FirstName, taskMember.Member.Employee.MiddleName, taskMember.Member.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : null;

            var inputVersions = await _context.ProjectTaskVersions
                .Where(v => v.ProjectTaskId == id)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

            var noteVersions = await _context.TaskNoteVersions
                .Where(n => n.ProjectTaskId == id)
                .OrderByDescending(n => n.VersionNumber)
                .ToListAsync();

            ViewBag.ProjectTaskVersions = inputVersions;
            ViewBag.TaskNoteVersions = noteVersions;

            var vm = new project_lifecycle.ViewModels.ProjectManager.ProjectTaskReviewViewModel
            {
                Id = task.Id,
                ProjectId = task.ProjectMilestone.ProjectId,
                ProjectMilestoneId = task.ProjectMilestoneId,
                Name = task.Name,
                Instructions = task.Instructions ?? string.Empty,
                EmployeeInput = task.Input,
                AssignedMemberName = assignedName,
                Status = task.Status,
                Notes = task.Notes
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Task(int id, string? Notes, string? action)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pmst => pmst.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            if (task.ProjectMilestone == null || task.ProjectMilestone.Project == null || task.ProjectMilestone.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            // Save old note as a version before overwriting
            var trimmedOld = (task.Notes ?? string.Empty).Trim();
            var trimmedNew = (Notes ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(trimmedOld) && trimmedOld != trimmedNew)
            {
                var existingNoteVersions = await _context.TaskNoteVersions
                    .Where(n => n.ProjectTaskId == id)
                    .ToListAsync();
                var nextNoteVersion = (existingNoteVersions.Any() ? existingNoteVersions.Max(n => n.VersionNumber) : 0) + 1;

                _context.TaskNoteVersions.Add(new project_lifecycle.Models.TaskNoteVersion
                {
                    ProjectTaskId = id,
                    VersionNumber = nextNoteVersion,
                    Note = task.Notes,
                    ProjectManagerId = pm.Id,
                    DateCreated = DateTime.Now
                });
            }

            // Update notes
            task.Notes = Notes;

            // Interpret action (button pressed)
            if (!string.IsNullOrEmpty(action))
            {
                if (action == "Checked")
                {
                    task.Status = "Checked";
                    if (task.CompletedAt == null)
                    {
                        task.CompletedAt = DateTime.Now;
                    }
                }
                else if (action == "RequireRevision")
                {
                    task.Status = "Require Revision";
                    // If reverting from Checked, clear CompletedAt so the timestamp reflects current state
                    task.CompletedAt = null;
                }
            }

            await _context.SaveChangesAsync();

            // After updating a task status, if all tasks under the milestone are Checked, mark the milestone Finished.
            var pmstId = task.ProjectMilestoneId;
            var tasksForMilestone = await _context.ProjectTasks
                .Where(t => t.ProjectMilestoneId == pmstId && !t.IsArchived)
                .ToListAsync();

            if (tasksForMilestone.Count > 0)
            {
                var allChecked = tasksForMilestone.All(t => string.Equals(t.Status, "Checked", StringComparison.OrdinalIgnoreCase));
                var pmst = await _context.ProjectMilestones.FirstOrDefaultAsync(p => p.Id == pmstId);
                if (pmst != null)
                {
                    if (allChecked && !string.Equals(pmst.Status, "Finished", StringComparison.OrdinalIgnoreCase))
                    {
                        pmst.Status = "Finished";
                        await _context.SaveChangesAsync();
                    }
                    else if (!allChecked && string.Equals(pmst.Status, "Finished", StringComparison.OrdinalIgnoreCase))
                    {
                        // If milestone was previously finished but a task is no longer checked, revert to Unfinished
                        pmst.Status = "Unfinished";
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // Sync parent project status based on all milestones
            if (task.ProjectMilestone?.ProjectId is int taskProjId)
                await SyncProjectStatusAsync(taskProjId);

            TempData["SuccessMessage"] = "Task updated.";
            await _audit.LogAsync(User, "Update", "Tasks", $"Updated task '{task.Name}' (ID: {task.Id})", "ProjectTask", task.Id.ToString());

            // Notify assigned employee(s) about the task review
            if (!string.IsNullOrEmpty(action))
            {
                var assignedMembers = await _context.TaskMembers
                    .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                    .Where(tm => tm.ProjectTaskId == id)
                    .ToListAsync();

                var (nTitle, nMsg, nType, nIcon) = action == "Checked"
                    ? ("Task Approved", $"Your task '{task.Name}' has been marked as checked.", "Success", "fas fa-circle-check")
                    : ("Revision Required", $"Your task '{task.Name}' requires revision.", "Warning", "fas fa-exclamation-triangle");

                foreach (var tm in assignedMembers)
                {
                    var empUserId = tm.Member?.Employee?.UserId;
                    if (!string.IsNullOrEmpty(empUserId))
                    {
                        await _notif.CreateAsync(empUserId, nTitle, nMsg, nType, nIcon,
                            $"/Employee/Project/Task/{task.Id}", "Task");
                    }
                }
            }

            // Note-only save: stay on the task page so the PM can see the updated note/version history
            if (string.IsNullOrEmpty(action))
                return RedirectToAction(nameof(Task), new { id });

            return RedirectToAction(nameof(Milestone), new { projectMilestoneId = task.ProjectMilestoneId });
        }

        [HttpGet]
        public async Task<IActionResult> EditTask(int id)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pmst => pmst.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            if (task.ProjectMilestone == null || task.ProjectMilestone.Project == null || task.ProjectMilestone.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            var projectId = task.ProjectMilestone.ProjectId;

            var members = await _context.Members
                .Where(m => m.ProjectId == projectId)
                .Include(m => m.Employee)
                .ToListAsync();

            var assigned = await _context.TaskMembers
                .Where(tm => tm.ProjectTaskId == id)
                .Select(tm => tm.MemberId)
                .ToListAsync();

            var vm = new ProjectTaskEditViewModel
            {
                Id = task.Id,
                ProjectId = projectId,
                ProjectName = task.ProjectMilestone.Project?.Name ?? string.Empty,
                ProjectMilestoneId = task.ProjectMilestoneId,
                Name = task.Name,
                Instructions = task.Instructions ?? string.Empty,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                AssignedMemberIds = assigned ?? new System.Collections.Generic.List<int>(),
                AvailableMembers = members.Select(m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = m.Employee != null ? string.Join(" ", new[] { m.Employee.FirstName, m.Employee.MiddleName, m.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : "N/A"
                }).ToList()
            };

            ViewData["ProjectStart"] = task.ProjectMilestone.Project?.StartDate.ToString("yyyy-MM-dd") ?? string.Empty;
            ViewData["ProjectEnd"] = task.ProjectMilestone.Project?.EndDate.ToString("yyyy-MM-dd") ?? string.Empty;

            return View("Edit", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Backwards-friendly route: forward to EditTask implementation
            return await EditTask(id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTask(int id, ProjectTask input, int[]? assignedMemberIds)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone).ThenInclude(pmst => pmst.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            if (task.ProjectMilestone == null || task.ProjectMilestone.Project == null || task.ProjectMilestone.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            if (input.EndDate < input.StartDate)
            {
                TempData["ErrorMessage"] = "End date must be on or after start date.";
                return RedirectToAction(nameof(EditTask), new { id });
            }

            // Update fields
            task.Name = input.Name?.Trim() ?? task.Name;
            task.Instructions = input.Instructions ?? string.Empty;
            task.StartDate = input.StartDate;
            task.EndDate = input.EndDate;

            // Update assigned members
            var selectedIds = (assignedMemberIds ?? System.Array.Empty<int>()).Where(x => x > 0).Distinct().ToArray();
            var validMembers = await _context.Members.Where(m => m.ProjectId == task.ProjectMilestone.ProjectId && selectedIds.Contains(m.Id)).ToListAsync();

            // Remove existing task members
            var existing = await _context.TaskMembers.Where(tm => tm.ProjectTaskId == id).ToListAsync();
            _context.TaskMembers.RemoveRange(existing);

            // Add new task members
            foreach (var m in validMembers)
            {
                _context.TaskMembers.Add(new Models.TaskMember { ProjectTaskId = task.Id, MemberId = m.Id, DateCreated = System.DateTime.Now });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task updated.";
            await _audit.LogAsync(User, "Update", "Tasks", $"Edited task '{task.Name}' (ID: {task.Id})", "ProjectTask", task.Id.ToString());

            return RedirectToAction(nameof(Milestone), new { projectMilestoneId = task.ProjectMilestoneId });
        }

        [HttpGet]
        public async Task<IActionResult> TaskVersion(int id)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var version = await _context.ProjectTaskVersions
                .Include(v => v.ProjectTask)
                    .ThenInclude(t => t!.ProjectMilestone).ThenInclude(ms => ms!.Project)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();

            if (version.ProjectTask?.ProjectMilestone?.Project?.ProjectManagerId != pm.Id)
                return Forbid();

            return View(version);
        }

        [HttpGet]
        public async Task<IActionResult> TaskNote(int id)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var note = await _context.TaskNoteVersions
                .Include(n => n.ProjectTask)
                    .ThenInclude(t => t!.ProjectMilestone).ThenInclude(ms => ms!.Project)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null) return NotFound();

            if (note.ProjectTask?.ProjectMilestone?.Project?.ProjectManagerId != pm.Id)
                return Forbid();

            return View(note);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(int projectMilestoneId, ProjectTask input, int[]? assignedMemberIds)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var pmst = await _context.ProjectMilestones.Include(p => p.Project).FirstOrDefaultAsync(p => p.Id == projectMilestoneId && p.Project != null && p.Project.ProjectManagerId == pm.Id);
            if (pmst == null) return NotFound();

            var selectedMemberIds = (assignedMemberIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToArray();
            if (selectedMemberIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select at least one member to assign.";
                return RedirectToAction(nameof(Milestone), new { projectMilestoneId });
            }

            var members = await _context.Members.Where(m => selectedMemberIds.Contains(m.Id) && m.ProjectId == pmst.ProjectId).ToListAsync();
            if (members.Count == 0)
            {
                TempData["ErrorMessage"] = "Selected members are not valid for this project.";
                return RedirectToAction(nameof(Milestone), new { projectMilestoneId });
            }

            if (input.EndDate < input.StartDate)
            {
                TempData["ErrorMessage"] = "End date must be on or after start date.";
                return RedirectToAction(nameof(Milestone), new { projectMilestoneId });
            }

            var task = new ProjectTask
            {
                ProjectMilestoneId = pmst.Id,
                Name = input.Name?.Trim() ?? string.Empty,
                Input = input.Input,
                Instructions = input.Instructions ?? string.Empty,
                Notes = input.Notes,
                Status = string.IsNullOrWhiteSpace(input.Status) ? "Pending" : input.Status,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                ProjectManagerId = pm.Id,
                DateCreated = DateTime.Now
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();

            foreach (var m in members)
            {
                var tm = new Models.TaskMember
                {
                    ProjectTaskId = task.Id,
                    MemberId = m.Id,
                    DateCreated = DateTime.Now
                };
                _context.TaskMembers.Add(tm);
            }
            await _context.SaveChangesAsync();

            // If the milestone was previously marked Finished, adding a new task
            // (which defaults to Pending) should revert the milestone to Unfinished.
            if (pmst != null && string.Equals(pmst.Status, "Finished", StringComparison.OrdinalIgnoreCase))
            {
                pmst.Status = "Unfinished";
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Task created and assigned.";
            await _audit.LogAsync(User, "Create", "Tasks", $"Created task '{input.Name}' (ID: {input.Id}) in milestone {projectMilestoneId}", "ProjectTask", input.Id.ToString());

            // Notify assigned members about the new task
            foreach (var m in members)
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == m.EmployeeId);
                if (emp != null && !string.IsNullOrEmpty(emp.UserId))
                {
                    await _notif.CreateAsync(emp.UserId,
                        "New Task Assigned",
                        $"You have been assigned to task '{task.Name}' in project.",
                        "Info", "fas fa-clipboard-list",
                        $"/Employee/Project/Task/{task.Id}",
                        "Task");
                }
            }

            return RedirectToAction(nameof(Milestone), new { projectMilestoneId });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddMember([FromForm] Member input)
        {
            var resolvedProjectId = GetPostedInt("ProjectId", "projectId") ?? input.ProjectId;
            var resolvedEmployeeId = GetPostedInt("EmployeeId", "employeeId") ?? input.EmployeeId;
            var resolvedProjectRoleId = GetPostedInt("ProjectRoleId", "projectRoleId") ?? input.ProjectRoleId;
            var resolvedMemberId = GetPostedInt("MemberId", "memberId") ?? input.Id;

            _logger.LogInformation("AddMember called with projectId={ProjectId}, employeeId={EmployeeId}, projectRoleId={RoleId}, memberId={MemberId}", resolvedProjectId, resolvedEmployeeId, resolvedProjectRoleId, resolvedMemberId);
            var pm = await GetCurrentProjectManagerAsync();
            _logger.LogDebug("Current PM: {PMId}", pm?.Id);
            if (pm == null) return Challenge();

            if (resolvedProjectId <= 0 || resolvedEmployeeId <= 0 || resolvedProjectRoleId <= 0)
            {
                TempData["ErrorMessage"] = "Please select an employee and role.";
                return RedirectToAction(nameof(Index));
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == resolvedProjectId && p.ProjectManagerId == pm.Id);
            if (project == null) return Forbid();

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == resolvedEmployeeId && e.DepartmentId == pm.DepartmentId);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Selected employee is not valid.";
                return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
            }

            var role = await _context.ProjectRoles.FindAsync(resolvedProjectRoleId);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Selected role is not valid.";
                return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
            }

            // Check if editing or creating
            if (resolvedMemberId > 0)
            {
                // Edit mode
                var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == resolvedMemberId && m.ProjectId == resolvedProjectId);
                if (member == null)
                {
                    TempData["ErrorMessage"] = "Member not found.";
                    return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
                }

                member.EmployeeId = resolvedEmployeeId;
                member.ProjectRoleId = resolvedProjectRoleId;

                try
                {
                    _context.Members.Update(member);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Member updated successfully.";
                    await _audit.LogAsync(User, "Update", "Project Members", $"Updated member (ID: {member.Id}) in project {member.ProjectId}", "Member", member.Id.ToString());
                    _logger.LogInformation("Member {MemberId} updated in project {ProjectId}", member.Id, member.ProjectId);
                }
                catch (DbUpdateException dbEx)
                {
                    var detail = BuildExceptionDetails(dbEx);
                    _logger.LogError(dbEx, "Error updating member {MemberId}. Details: {Details}", resolvedMemberId, detail);
                    TempData["ErrorMessage"] = "Failed to update member: " + detail;
                }
                catch (Exception ex)
                {
                    var detail = BuildExceptionDetails(ex);
                    _logger.LogError(ex, "Error updating member {MemberId}. Details: {Details}", resolvedMemberId, detail);
                    TempData["ErrorMessage"] = "Failed to update member: " + detail;
                }
            }
            else
            {
                // Create mode
                var exists = await _context.Members.AnyAsync(m => m.ProjectId == resolvedProjectId && m.EmployeeId == resolvedEmployeeId);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Employee is already a member of the project.";
                    return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
                }

                var member = new Member
                {
                    ProjectId = resolvedProjectId,
                    EmployeeId = resolvedEmployeeId,
                    ProjectRoleId = resolvedProjectRoleId,
                    DateCreated = DateTime.Now
                };

                try
                {
                    _context.Members.Add(member);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Member added to project.";
                    await _audit.LogAsync(User, "Create", "Project Members", $"Added member (ID: {member.Id}) to project {member.ProjectId}", "Member", member.Id.ToString());

                    // Notify the added employee
                    if (employee != null && !string.IsNullOrEmpty(employee.UserId))
                    {
                        await _notif.CreateAsync(employee.UserId,
                            "Added to Project",
                            $"You have been added to project '{project.Name}'.",
                            "Info", "fas fa-user-plus",
                            $"/Employee/Project/Details/{project.Id}",
                            "Project");
                    }

                    _logger.LogInformation("Member {MemberId} added to project {ProjectId} (employee {EmployeeId})", member.Id, member.ProjectId, member.EmployeeId);
                }
                catch (DbUpdateException dbEx)
                {
                    var detail = BuildExceptionDetails(dbEx);
                    _logger.LogError(dbEx, "Error adding member to project {ProjectId}. Details: {Details}", resolvedProjectId, detail);
                    TempData["ErrorMessage"] = "Failed to add member: " + detail;
                }
                catch (Exception ex)
                {
                    var detail = BuildExceptionDetails(ex);
                    _logger.LogError(ex, "Error adding member to project {ProjectId}. Details: {Details}", resolvedProjectId, detail);
                    TempData["ErrorMessage"] = "Failed to add member: " + detail;
                }
            }

            return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
        }

        private string BuildExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();
            void Append(Exception e)
            {
                if (e == null) return;
                sb.AppendLine($"{e.GetType().Name}: {e.Message}");
                if (e is DbUpdateException dbu && dbu.Entries != null)
                {
                    foreach (var entry in dbu.Entries)
                    {
                        sb.AppendLine($"Entity: {entry.Entity?.GetType().FullName ?? "<null>"}, State: {entry.State}");
                    }
                }
                if (e.InnerException != null)
                {
                    sb.AppendLine("-- InnerException --");
                    Append(e.InnerException);
                }
            }

            Append(ex);
            var result = sb.ToString();
            if (string.IsNullOrWhiteSpace(result)) return ex.Message;
            // truncate to a reasonable length for TempData/UI display
            return result.Length > 2000 ? result.Substring(0, 2000) + "..." : result;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddMilestone([FromForm] ProjectMilestone input)
        {
            var resolvedProjectId = GetPostedInt("ProjectId", "projectId") ?? input.ProjectId;
            var resolvedMilestoneId = GetPostedInt("MilestoneId", "milestoneId") ?? input.MilestoneId;
            var resolvedSequenceOrder = GetPostedInt("SequenceOrder", "sequenceOrder") ?? input.SequenceOrder;
            var resolvedProjectMilestoneId = GetPostedInt("ProjectMilestoneId", "projectMilestoneId") ?? input.Id;

            _logger.LogInformation("AddMilestone called with projectId={ProjectId}, milestoneId={MilestoneId}, sequenceOrder={Sequence}, projectMilestoneId={ProjectMilestoneId}", resolvedProjectId, resolvedMilestoneId, resolvedSequenceOrder, resolvedProjectMilestoneId);
            var pm = await GetCurrentProjectManagerAsync();
            _logger.LogDebug("Current PM: {PMId}", pm?.Id);
            if (pm == null) return Challenge();

            if (resolvedProjectId <= 0 || resolvedMilestoneId <= 0 || resolvedSequenceOrder <= 0)
            {
                _logger.LogWarning("Invalid parameters for AddMilestone: projectId={ProjectId}, milestoneId={MilestoneId}, sequenceOrder={Sequence}", resolvedProjectId, resolvedMilestoneId, resolvedSequenceOrder);
                TempData["ErrorMessage"] = "Please select a milestone and provide a valid order.";
                return RedirectToAction(nameof(Index));
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == resolvedProjectId && p.ProjectManagerId == pm.Id);
            if (project == null) return Forbid();

            var milestone = await _context.Milestones.FindAsync(resolvedMilestoneId);
            if (milestone == null)
            {
                TempData["ErrorMessage"] = "Selected milestone is not valid.";
                return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
            }

            // Check if editing or creating
            if (resolvedProjectMilestoneId > 0)
            {
                // Edit mode
                var projectMilestone = await _context.ProjectMilestones.FirstOrDefaultAsync(pm => pm.Id == resolvedProjectMilestoneId && pm.ProjectId == resolvedProjectId);
                if (projectMilestone == null)
                {
                    TempData["ErrorMessage"] = "Milestone not found in project.";
                    return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
                }

                projectMilestone.MilestoneId = resolvedMilestoneId;
                projectMilestone.SequenceOrder = resolvedSequenceOrder;

                try
                {
                    _context.ProjectMilestones.Update(projectMilestone);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Milestone updated successfully.";
                    await _audit.LogAsync(User, "Update", "Project Milestones", $"Updated milestone (ID: {projectMilestone.Id}) in project {projectMilestone.ProjectId}", "ProjectMilestone", projectMilestone.Id.ToString());
                    _logger.LogInformation("ProjectMilestone {Id} updated in project {ProjectId}", projectMilestone.Id, projectMilestone.ProjectId);
                }
                catch (DbUpdateException dbEx)
                {
                    var detail = BuildExceptionDetails(dbEx);
                    _logger.LogError(dbEx, "Error updating milestone {MilestoneId}. Details: {Details}", resolvedProjectMilestoneId, detail);
                    TempData["ErrorMessage"] = "Failed to update milestone: " + detail;
                }
                catch (Exception ex)
                {
                    var detail = BuildExceptionDetails(ex);
                    _logger.LogError(ex, "Error updating milestone {MilestoneId}. Details: {Details}", resolvedProjectMilestoneId, detail);
                    TempData["ErrorMessage"] = "Failed to update milestone: " + detail;
                }
            }
            else
            {
                // Create mode
                var duplicate = await _context.ProjectMilestones.AnyAsync(pmst => pmst.ProjectId == resolvedProjectId && pmst.MilestoneId == resolvedMilestoneId);
                if (duplicate)
                {
                    TempData["ErrorMessage"] = "Milestone already added to this project.";
                    return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
                }

                var projectMilestone = new ProjectMilestone
                {
                    ProjectId = resolvedProjectId,
                    MilestoneId = resolvedMilestoneId,
                    SequenceOrder = resolvedSequenceOrder,
                    Status = "Unfinished",
                    DateCreated = DateTime.Now
                };

                try
                {
                    _context.ProjectMilestones.Add(projectMilestone);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Milestone added to project.";
                    await _audit.LogAsync(User, "Create", "Project Milestones", $"Added milestone (ID: {projectMilestone.Id}) to project {projectMilestone.ProjectId}", "ProjectMilestone", projectMilestone.Id.ToString());
                    _logger.LogInformation("ProjectMilestone {Id} added to project {ProjectId} (milestone {MilestoneId})", projectMilestone.Id, projectMilestone.ProjectId, projectMilestone.MilestoneId);
                }
                catch (DbUpdateException dbEx)
                {
                    var detail = BuildExceptionDetails(dbEx);
                    _logger.LogError(dbEx, "Error adding milestone to project {ProjectId}. Details: {Details}", resolvedProjectId, detail);
                    TempData["ErrorMessage"] = "Failed to add milestone: " + detail;
                }
                catch (Exception ex)
                {
                    var detail = BuildExceptionDetails(ex);
                    _logger.LogError(ex, "Error adding milestone to project {ProjectId}. Details: {Details}", resolvedProjectId, detail);
                    TempData["ErrorMessage"] = "Failed to add milestone: " + detail;
                }
            }

            return RedirectToAction(nameof(Details), new { id = resolvedProjectId });
        }

        private int? GetPostedInt(params string[] keys)
        {
            if (!Request.HasFormContentType) return null;

            foreach (var key in keys)
            {
                if (Request.Form.TryGetValue(key, out var value) && int.TryParse(value.FirstOrDefault(), out var parsed))
                {
                    return parsed;
                }
            }

            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int memberId)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var member = await _context.Members.Include(m => m.Project).FirstOrDefaultAsync(m => m.Id == memberId);
            if (member == null) return NotFound();

            if (member.Project == null || member.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            _context.Members.Remove(member);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Delete", "Project Members", $"Removed member (ID: {memberId}) from project {member.Project!.Id}", "Member", memberId.ToString());

            TempData["SuccessMessage"] = "Member removed from project.";
            return RedirectToAction(nameof(Details), new { id = member.Project!.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProjectMilestone(int projectMilestoneId)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var pmst = await _context.ProjectMilestones.Include(p => p.Project).FirstOrDefaultAsync(p => p.Id == projectMilestoneId);
            if (pmst == null) return NotFound();

            if (pmst.Project == null || pmst.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            // If the milestone being removed is active (not archived), adjust
            // the sequence order of later active milestones so ordering remains contiguous.
            if (!pmst.IsArchived)
            {
                var originalOrder = pmst.SequenceOrder;
                var laterActive = await _context.ProjectMilestones
                    .Where(x => x.ProjectId == pmst.ProjectId && !x.IsArchived && x.SequenceOrder > originalOrder)
                    .ToListAsync();

                foreach (var m in laterActive)
                {
                    m.SequenceOrder -= 1;
                    _context.ProjectMilestones.Update(m);
                }
            }

            _context.ProjectMilestones.Remove(pmst);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Delete", "Project Milestones", $"Removed milestone (ID: {projectMilestoneId}) from project {pmst.Project!.Id}", "ProjectMilestone", projectMilestoneId.ToString());

            TempData["SuccessMessage"] = "Milestone removed from project.";
            return RedirectToAction(nameof(Details), new { id = pmst.Project!.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveProjectMilestone(int projectMilestoneId)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var pmst = await _context.ProjectMilestones.Include(p => p.Project).FirstOrDefaultAsync(p => p.Id == projectMilestoneId);
            if (pmst == null) return NotFound();

            if (pmst.Project == null || pmst.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            // If already archived, nothing to do
            if (pmst.IsArchived)
            {
                TempData["ErrorMessage"] = "Milestone is already archived.";
                return RedirectToAction(nameof(Details), new { id = pmst.Project!.Id });
            }

            // When archiving a milestone, remove it from the active sequence by
            // decrementing the SequenceOrder of any later active milestones so
            // the remaining active milestones keep contiguous ordering.
            var originalOrder = pmst.SequenceOrder;

            var laterActive = await _context.ProjectMilestones
                .Where(x => x.ProjectId == pmst.ProjectId && !x.IsArchived && x.SequenceOrder > originalOrder)
                .ToListAsync();

            foreach (var m in laterActive)
            {
                m.SequenceOrder -= 1;
                _context.ProjectMilestones.Update(m);
            }

            // Mark the milestone archived and remove it from ordering (set to 0)
            pmst.IsArchived = true;
            pmst.SequenceOrder = 0;
            _context.ProjectMilestones.Update(pmst);

            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Archive", "Project Milestones", $"Archived milestone (ID: {projectMilestoneId}) in project {pmst.Project!.Id}", "ProjectMilestone", projectMilestoneId.ToString());

            TempData["SuccessMessage"] = "Milestone archived.";
            return RedirectToAction(nameof(Details), new { id = pmst.Project!.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveProjectMilestone(int projectMilestoneId)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var pmst = await _context.ProjectMilestones.Include(p => p.Project).FirstOrDefaultAsync(p => p.Id == projectMilestoneId);
            if (pmst == null) return NotFound();

            if (pmst.Project == null || pmst.Project.ProjectManagerId != pm.Id)
            {
                return Forbid();
            }

            // If not archived, nothing to do
            if (!pmst.IsArchived)
            {
                TempData["ErrorMessage"] = "Milestone is not archived.";
                return RedirectToAction(nameof(Details), new { id = pmst.Project!.Id });
            }

            // When unarchiving, place the milestone at the end of active sequence
            // so existing sequence ordering is preserved. EF Core cannot translate
            // DefaultIfEmpty + Max in all cases, so compute in two steps.
            var activeQuery = _context.ProjectMilestones
                .Where(x => x.ProjectId == pmst.ProjectId && !x.IsArchived)
                .Select(x => x.SequenceOrder);

            var hasActive = await activeQuery.AnyAsync();
            var maxOrder = hasActive ? await activeQuery.MaxAsync() : 0;

            pmst.IsArchived = false;
            pmst.SequenceOrder = maxOrder + 1;
            _context.ProjectMilestones.Update(pmst);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Unarchive", "Project Milestones", $"Unarchived milestone (ID: {projectMilestoneId}) in project {pmst.Project!.Id}", "ProjectMilestone", projectMilestoneId.ToString());

            TempData["SuccessMessage"] = "Milestone unarchived.";
            return RedirectToAction(nameof(Details), new { id = pmst.Project!.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ProjectManagerId == pm.Id);
            if (project == null) return NotFound();

            project.IsArchived = true;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Archive", "Projects", $"Archived project '{project.Name}' (ID: {project.Id})", "Project", project.Id.ToString());

            TempData["SuccessMessage"] = "Project archived successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            var pm = await GetCurrentProjectManagerAsync();
            if (pm == null) return Challenge();

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ProjectManagerId == pm.Id);
            if (project == null) return NotFound();

            project.IsArchived = false;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Unarchive", "Projects", $"Unarchived project '{project.Name}' (ID: {project.Id})", "Project", project.Id.ToString());

            TempData["SuccessMessage"] = "Project unarchived successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadRichText(IFormFile upload)
        {
            try
            {
                if (upload == null || upload.Length == 0)
                {
                    return BadRequest(new { error = new { message = "No file uploaded." } });
                }

                const long maxFileSize = 10 * 1024 * 1024;
                if (upload.Length > maxFileSize)
                {
                    return BadRequest(new { error = new { message = "File too large. Max size is 10 MB." } });
                }

                var allowedExtensions = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg",
                    ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".zip"
                };

                var extension = Path.GetExtension(upload.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = new { message = "Unsupported file type." } });
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "editor");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var safeOriginalName = Path.GetFileName(upload.FileName);
                var fileName = $"{Guid.NewGuid()}_{safeOriginalName}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }

                var publicUrl = Url.Content($"~/uploads/editor/{fileName}") ?? $"/uploads/editor/{fileName}";
                return Json(new
                {
                    url = publicUrl,
                    fileName = safeOriginalName,
                    isImage = upload.ContentType.StartsWith("image/", System.StringComparison.OrdinalIgnoreCase)
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in UploadRichText");
                return StatusCode(500, new { error = new { message = "Upload failed." } });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHolidays(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
            {
                return Json(new List<object>());
            }

            // Limit range to 2 years max to avoid excessive API calls
            if ((endDate - startDate).TotalDays > 730)
            {
                endDate = startDate.AddDays(730);
            }

            var holidays = await _holidayService.GetHolidaysAsync(startDate, endDate);

            var result = holidays.Select(h => new
            {
                date = h.Date.ToString("yyyy-MM-dd"),
                localName = h.LocalName,
                name = h.Name,
                type = h.Type
            });

            return Json(result);
        }
    }
}
