using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;
using project_lifecycle.ViewModels.DepartmentHead;

namespace project_lifecycle.DepartmentHeadArea.Controllers
{
    [Area("DepartmentHead")]
    [Authorize(Roles = "DepartmentHead")]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INagerHolidayService _holidayService;
        private readonly IAuditLogService _audit;
        private readonly INotificationService _notif;

        public ProjectController(ApplicationDbContext context, INagerHolidayService holidayService, IAuditLogService audit, INotificationService notif)
        {
            _context = context;
            _holidayService = holidayService;
            _audit = audit;
            _notif = notif;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null)
            {
                return Challenge();
            }

            var model = await BuildIndexViewModelAsync(dh);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();
            var model = await BuildIndexViewModelAsync(dh);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "CreateProject")] CreateDepartmentHeadProjectViewModel createModel)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null)
            {
                return Challenge();
            }

            if (createModel.EndDate < createModel.StartDate)
            {
                ModelState.AddModelError("CreateProject.EndDate", "End date must be on or after the start date.");
            }

            var availableProposalIds = await _context.ProjectProposals
                .Include(p => p.Employee)
                .Where(p => p.Status == "Approved"
                    && p.Employee != null
                    && p.Employee.DepartmentId == dh.DepartmentId
                    && !_context.Projects.Any(pr => pr.ProjectProposalId == p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (!availableProposalIds.Contains(createModel.ProjectProposalId))
            {
                ModelState.AddModelError("CreateProject.ProjectProposalId", "Please select a valid approved proposal.");
            }

            var availableProjectManagerIds = await _context.ProjectManagers
                .Select(pm => pm.Id)
                .ToListAsync();

            if (!availableProjectManagerIds.Contains(createModel.ProjectManagerId))
            {
                ModelState.AddModelError("CreateProject.ProjectManagerId", "Please select a valid project manager.");
            }

            var availableEmployeeIds = await _context.Employees
                .Select(e => e.Id)
                .ToListAsync();

            var availableRoleIds = await _context.ProjectRoles
                .Select(r => r.Id)
                .ToListAsync();

            var memberPairs = new List<(int EmployeeId, int RoleId)>();
            var employeeIds = createModel.MemberEmployeeIds ?? new List<int>();
            var roleIds = createModel.MemberProjectRoleIds ?? new List<int>();

            if (employeeIds.Count != roleIds.Count)
            {
                ModelState.AddModelError(string.Empty, "Member selection is invalid. Please try again.");
            }
            else
            {
                for (var i = 0; i < employeeIds.Count; i++)
                {
                    var employeeId = employeeIds[i];
                    var roleId = roleIds[i];

                    if (employeeId <= 0 && roleId <= 0)
                    {
                        continue;
                    }

                    if (employeeId <= 0 || roleId <= 0)
                    {
                        ModelState.AddModelError(string.Empty, "Each selected member must have both an employee and a project role.");
                        continue;
                    }

                    if (!availableEmployeeIds.Contains(employeeId))
                    {
                        ModelState.AddModelError(string.Empty, "One or more selected members are not valid for your department.");
                        continue;
                    }

                    if (!availableRoleIds.Contains(roleId))
                    {
                        ModelState.AddModelError(string.Empty, "One or more selected project roles are invalid.");
                        continue;
                    }

                    memberPairs.Add((employeeId, roleId));
                }
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildIndexViewModelAsync(dh, createModel);
                return View(invalidModel);
            }

            var project = new Project
            {
                Name = createModel.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(createModel.Description) ? null : createModel.Description.Trim(),
                ProjectProposalId = createModel.ProjectProposalId,
                ProjectManagerId = createModel.ProjectManagerId,
                StartDate = createModel.StartDate,
                EndDate = createModel.EndDate,
                DateCreated = DateTime.Now
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var uniqueMembers = memberPairs
                .GroupBy(m => m.EmployeeId)
                .Select(g => g.First())
                .ToList();

            if (uniqueMembers.Any())
            {
                var memberEntities = uniqueMembers.Select(m => new Member
                {
                    ProjectId = project.Id,
                    EmployeeId = m.EmployeeId,
                    ProjectRoleId = m.RoleId,
                    DateCreated = DateTime.Now
                }).ToList();

                _context.Members.AddRange(memberEntities);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Project created successfully.";
            await _audit.LogAsync(User, "Create", "Projects", $"Created project '{project.Name}' (ID: {project.Id})", "Project", project.Id.ToString());

            // Notify the assigned project manager
            var pmEntity = await _context.ProjectManagers.FirstOrDefaultAsync(p => p.Id == project.ProjectManagerId);
            if (pmEntity != null && !string.IsNullOrEmpty(pmEntity.UserId))
            {
                await _notif.CreateAsync(pmEntity.UserId,
                    "New Project Assigned",
                    $"You have been assigned as project manager for '{project.Name}'.",
                    "Success", "fas fa-diagram-project",
                    $"/ProjectManager/Project/Details/{project.Id}",
                    "Project");
            }

            // Notify team members added to the project
            foreach (var mp in uniqueMembers)
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == mp.EmployeeId);
                if (emp != null && !string.IsNullOrEmpty(emp.UserId))
                {
                    await _notif.CreateAsync(emp.UserId,
                        "Added to Project",
                        $"You have been added to project '{project.Name}'.",
                        "Info", "fas fa-users",
                        $"/Employee/Project/Details/{project.Id}",
                        "Project");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var project = await _context.Projects
                .Where(p => p.Id == id && _context.ProjectProposals.Any(pp =>
                    pp.Id == p.ProjectProposalId && pp.Employee != null && pp.Employee.DepartmentId == dh.DepartmentId))
                .FirstOrDefaultAsync();

            if (project == null) return NotFound();

            var members = await _context.Members
                .Where(m => m.ProjectId == id)
                .OrderBy(m => m.Id)
                .ToListAsync();

            var editModel = new EditDepartmentHeadProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                ProjectProposalId = project.ProjectProposalId,
                ProjectManagerId = project.ProjectManagerId,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                MemberEmployeeIds = members.Select(m => m.EmployeeId).ToList(),
                MemberProjectRoleIds = members.Select(m => m.ProjectRoleId).ToList()
            };

            var vm = await BuildEditViewModelAsync(dh, id, editModel);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind(Prefix = "EditProject")] EditDepartmentHeadProjectViewModel editModel)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var project = await _context.Projects
                .Where(p => p.Id == id && _context.ProjectProposals.Any(pp =>
                    pp.Id == p.ProjectProposalId && pp.Employee != null && pp.Employee.DepartmentId == dh.DepartmentId))
                .FirstOrDefaultAsync();

            if (project == null) return NotFound();

            if (editModel.EndDate < editModel.StartDate)
            {
                ModelState.AddModelError("EditProject.EndDate", "End date must be on or after the start date.");
            }

            var availableProposalIds = await _context.ProjectProposals
                .Include(p => p.Employee)
                .Where(p => p.Status == "Approved"
                    && p.Employee != null
                    && p.Employee.DepartmentId == dh.DepartmentId
                    && (!_context.Projects.Any(pr => pr.ProjectProposalId == p.Id) || p.Id == project.ProjectProposalId))
                .Select(p => p.Id)
                .ToListAsync();

            if (!availableProposalIds.Contains(editModel.ProjectProposalId))
            {
                ModelState.AddModelError("EditProject.ProjectProposalId", "Please select a valid approved proposal.");
            }

            var availableProjectManagerIds = await _context.ProjectManagers
                .Select(pm => pm.Id)
                .ToListAsync();

            if (!availableProjectManagerIds.Contains(editModel.ProjectManagerId))
            {
                ModelState.AddModelError("EditProject.ProjectManagerId", "Please select a valid project manager.");
            }

            var availableEmployeeIds = await _context.Employees
                .Select(e => e.Id)
                .ToListAsync();

            var availableRoleIds = await _context.ProjectRoles
                .Select(r => r.Id)
                .ToListAsync();

            var memberPairs = new List<(int EmployeeId, int RoleId)>();
            var employeeIds = editModel.MemberEmployeeIds ?? new List<int>();
            var roleIds = editModel.MemberProjectRoleIds ?? new List<int>();

            if (employeeIds.Count != roleIds.Count)
            {
                ModelState.AddModelError(string.Empty, "Member selection is invalid. Please try again.");
            }
            else
            {
                for (var i = 0; i < employeeIds.Count; i++)
                {
                    var employeeId = employeeIds[i];
                    var roleId = roleIds[i];

                    if (employeeId <= 0 && roleId <= 0)
                    {
                        continue;
                    }

                    if (employeeId <= 0 || roleId <= 0)
                    {
                        ModelState.AddModelError(string.Empty, "Each selected member must have both an employee and a project role.");
                        continue;
                    }

                    if (!availableEmployeeIds.Contains(employeeId))
                    {
                        ModelState.AddModelError(string.Empty, "One or more selected members are not valid.");
                        continue;
                    }

                    if (!availableRoleIds.Contains(roleId))
                    {
                        ModelState.AddModelError(string.Empty, "One or more selected project roles are invalid.");
                        continue;
                    }

                    memberPairs.Add((employeeId, roleId));
                }
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildEditViewModelAsync(dh, id, editModel);
                return View(invalidModel);
            }

            var oldProjectManagerId = project.ProjectManagerId;

            project.Name = editModel.Name.Trim();
            project.Description = string.IsNullOrWhiteSpace(editModel.Description) ? null : editModel.Description.Trim();
            project.ProjectProposalId = editModel.ProjectProposalId;
            project.ProjectManagerId = editModel.ProjectManagerId;
            project.StartDate = editModel.StartDate;
            project.EndDate = editModel.EndDate;

            var existingMembers = await _context.Members.Where(m => m.ProjectId == id).ToListAsync();
            _context.Members.RemoveRange(existingMembers);

            var uniqueMembers = memberPairs
                .GroupBy(m => m.EmployeeId)
                .Select(g => g.First())
                .ToList();

            if (uniqueMembers.Any())
            {
                var memberEntities = uniqueMembers.Select(m => new Member
                {
                    ProjectId = id,
                    EmployeeId = m.EmployeeId,
                    ProjectRoleId = m.RoleId,
                    DateCreated = DateTime.Now
                }).ToList();

                _context.Members.AddRange(memberEntities);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Project updated successfully.";
            await _audit.LogAsync(User, "Edit", "Projects", $"Updated project '{project.Name}' (ID: {id})", "Project", id.ToString());

            // Notify the new project manager if changed
            if (editModel.ProjectManagerId != oldProjectManagerId)
            {
                var pmEntity = await _context.ProjectManagers.FirstOrDefaultAsync(p => p.Id == editModel.ProjectManagerId);
                if (pmEntity != null && !string.IsNullOrEmpty(pmEntity.UserId))
                {
                    await _notif.CreateAsync(pmEntity.UserId,
                        "New Project Assigned",
                        $"You have been assigned as project manager for '{project.Name}'.",
                        "Success", "fas fa-diagram-project",
                        $"/ProjectManager/Project/Details/{id}",
                        "Project");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveProject(int id)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var project = await _context.Projects
                .Where(p => p.Id == id && _context.ProjectProposals.Any(pp =>
                    pp.Id == p.ProjectProposalId && pp.Employee != null && pp.Employee.DepartmentId == dh.DepartmentId))
                .FirstOrDefaultAsync();
            if (project == null) return NotFound();

            project.IsArchived = true;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Archive", "Projects", $"Archived project '{project.Name}' (ID: {project.Id})", "Project", project.Id.ToString());
            TempData["SuccessMessage"] = "Project archived successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveProject(int id)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var project = await _context.Projects
                .Where(p => p.Id == id && _context.ProjectProposals.Any(pp =>
                    pp.Id == p.ProjectProposalId && pp.Employee != null && pp.Employee.DepartmentId == dh.DepartmentId))
                .FirstOrDefaultAsync();
            if (project == null) return NotFound();

            project.IsArchived = false;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User, "Unarchive", "Projects", $"Unarchived project '{project.Name}' (ID: {project.Id})", "Project", project.Id.ToString());
            TempData["SuccessMessage"] = "Project unarchived successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Show(int id)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var project = await _context.Projects
                .Where(p => p.Id == id && _context.ProjectProposals.Any(pp =>
                    pp.Id == p.ProjectProposalId && pp.Employee != null && pp.Employee.DepartmentId == dh.DepartmentId))
                .Select(p => new project_lifecycle.ViewModels.ProjectManager.ProjectDetailViewModel
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
                .Select(m => new project_lifecycle.ViewModels.ProjectManager.MemberViewModel
                {
                    Id = m.Id,
                    EmployeeId = m.EmployeeId,
                    EmployeeName = m.Employee != null ? string.Join(" ", new[] { m.Employee.FirstName, m.Employee.MiddleName, m.Employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))) : "N/A",
                    ProfileImage = m.Employee?.ProfileImage,
                    ProjectRoleId = m.ProjectRoleId,
                    ProjectRoleName = m.ProjectRole?.Name ?? "N/A"
                })
                .ToList();

            project.Milestones = milestones
                .Select(ms => new project_lifecycle.ViewModels.ProjectManager.ProjectMilestoneViewModel
                {
                    Id = ms.Id,
                    MilestoneId = ms.MilestoneId,
                    MilestoneName = ms.Milestone?.Name ?? "N/A",
                    SequenceOrder = ms.SequenceOrder,
                    Status = ms.Status,
                    IsArchived = ms.IsArchived
                })
                .ToList();

            var vm = new project_lifecycle.ViewModels.ProjectManager.ProjectManageViewModel
            {
                Project = project
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Milestone(int projectMilestoneId)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var pmst = await _context.ProjectMilestones
                .Include(p => p.Project).ThenInclude(p => p!.ProjectProposal).ThenInclude(p => p!.Employee)
                .Include(p => p.Milestone)
                .FirstOrDefaultAsync(p => p.Id == projectMilestoneId);

            if (pmst == null || pmst.Project?.ProjectProposal?.Employee?.DepartmentId != dh.DepartmentId)
                return NotFound();

            var tasks = await _context.ProjectTasks
                .Where(t => t.ProjectMilestoneId == pmst.Id)
                .ToListAsync();

            var taskIds = tasks.Select(t => t.Id).ToList();
            var taskMembers = await _context.TaskMembers
                .Where(tm => taskIds.Contains(tm.ProjectTaskId))
                .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                .ToListAsync();

            var vm = new project_lifecycle.ViewModels.ProjectManager.MilestoneViewModel
            {
                ProjectId = pmst.ProjectId,
                ProjectName = pmst.Project?.Name ?? string.Empty,
                ProjectMilestoneId = pmst.Id,
                MilestoneId = pmst.MilestoneId,
                MilestoneName = pmst.Milestone?.Name ?? string.Empty,
                SequenceOrder = pmst.SequenceOrder,
                Status = pmst.Status
            };

            vm.Tasks = tasks.Select(t =>
            {
                var firstMember = taskMembers.FirstOrDefault(tm => tm.ProjectTaskId == t.Id);
                var emp = firstMember?.Member?.Employee;
                var name = emp != null
                    ? string.Join(" ", new[] { emp.FirstName, emp.MiddleName, emp.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                    : null;
                return new project_lifecycle.ViewModels.ProjectManager.ProjectTaskItemViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Status = t.Status,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    AssignedMemberName = name,
                    IsArchived = t.IsArchived
                };
            }).ToList();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Task(int id)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var task = await _context.ProjectTasks
                .Include(t => t.ProjectMilestone)
                    .ThenInclude(pmst => pmst!.Project)
                        .ThenInclude(p => p!.ProjectProposal)
                            .ThenInclude(pp => pp!.Employee)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            if (task.ProjectMilestone?.Project?.ProjectProposal?.Employee?.DepartmentId != dh.DepartmentId)
                return Forbid();

            var taskMember = await _context.TaskMembers
                .Include(tm => tm.Member).ThenInclude(m => m.Employee)
                .FirstOrDefaultAsync(tm => tm.ProjectTaskId == id);

            var assignedName = taskMember?.Member?.Employee != null
                ? string.Join(" ", new[] {
                    taskMember.Member.Employee.FirstName,
                    taskMember.Member.Employee.MiddleName,
                    taskMember.Member.Employee.LastName
                  }.Where(x => !string.IsNullOrWhiteSpace(x)))
                : null;

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
                ProjectId = task.ProjectMilestone!.ProjectId,
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

        [HttpGet]
        public async Task<IActionResult> TaskVersion(int id)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var version = await _context.ProjectTaskVersions
                .Include(v => v.ProjectTask)
                    .ThenInclude(t => t!.ProjectMilestone)
                        .ThenInclude(ms => ms!.Project)
                            .ThenInclude(p => p!.ProjectProposal)
                                .ThenInclude(pp => pp!.Employee)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();

            if (version.ProjectTask?.ProjectMilestone?.Project?.ProjectProposal?.Employee?.DepartmentId != dh.DepartmentId)
                return Forbid();

            return View(version);
        }

        [HttpGet]
        public async Task<IActionResult> TaskNote(int id)
        {
            var dh = await GetCurrentDepartmentHeadAsync();
            if (dh == null) return Challenge();

            var note = await _context.TaskNoteVersions
                .Include(n => n.ProjectTask)
                    .ThenInclude(t => t!.ProjectMilestone)
                        .ThenInclude(ms => ms!.Project)
                            .ThenInclude(p => p!.ProjectProposal)
                                .ThenInclude(pp => pp!.Employee)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null) return NotFound();

            if (note.ProjectTask?.ProjectMilestone?.Project?.ProjectProposal?.Employee?.DepartmentId != dh.DepartmentId)
                return Forbid();

            return View(note);
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

        private async Task<DepartmentHead?> GetCurrentDepartmentHeadAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == userId);
        }

        private async Task<DepartmentHeadProjectIndexViewModel> BuildIndexViewModelAsync(DepartmentHead dh, CreateDepartmentHeadProjectViewModel? createModel = null)
        {
            var projectRows = await _context.Projects
                .Where(p => _context.ProjectProposals.Any(pp => pp.Id == p.ProjectProposalId
                    && pp.Employee != null
                    && pp.Employee!.DepartmentId == dh.DepartmentId))
                .OrderByDescending(p => p.DateCreated)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProposalTitle = p.ProjectProposal != null ? p.ProjectProposal.Title : string.Empty,
                    ProjectManagerFirstName = p.ProjectManager != null ? p.ProjectManager.FirstName : null,
                    ProjectManagerMiddleName = p.ProjectManager != null ? p.ProjectManager.MiddleName : null,
                    ProjectManagerLastName = p.ProjectManager != null ? p.ProjectManager.LastName : null,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    DateCreated = p.DateCreated,
                    IsArchived = p.IsArchived
                })
                .ToListAsync();

            var projectIds = projectRows.Select(p => p.Id).ToList();
            var memberCountByProjectId = await _context.Members
                .Where(m => projectIds.Contains(m.ProjectId))
                .GroupBy(m => m.ProjectId)
                .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

            var memberRows = await _context.Members
                .Where(m => projectIds.Contains(m.ProjectId))
                .Include(m => m.Employee)
                .Include(m => m.ProjectRole)
                .Select(m => new
                {
                    m.ProjectId,
                    m.Id,
                    m.EmployeeId,
                    EmployeeFirst = m.Employee != null ? m.Employee.FirstName : null,
                    EmployeeMiddle = m.Employee != null ? m.Employee.MiddleName : null,
                    EmployeeLast = m.Employee != null ? m.Employee.LastName : null,
                    ProfileImage = m.Employee != null ? m.Employee.ProfileImage : null,
                    m.ProjectRoleId,
                    ProjectRoleName = m.ProjectRole != null ? m.ProjectRole.Name : null
                })
                .ToListAsync();

            var membersByProjectId = memberRows
                .GroupBy(m => m.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(m => new ViewModels.DepartmentHead.MemberViewModel
                {
                    Id = m.Id,
                    EmployeeId = m.EmployeeId,
                    EmployeeName = BuildFullName(m.EmployeeFirst, m.EmployeeMiddle, m.EmployeeLast),
                    ProfileImage = m.ProfileImage,
                    ProjectRoleId = m.ProjectRoleId,
                    ProjectRoleName = m.ProjectRoleName ?? string.Empty
                }).ToList());

            var milestoneRows = await _context.ProjectMilestones
                .Where(pm => projectIds.Contains(pm.ProjectId))
                .Include(pm => pm.Milestone)
                .Select(pm => new
                {
                    pm.ProjectId,
                    pm.Id,
                    pm.MilestoneId,
                    MilestoneName = pm.Milestone != null ? pm.Milestone.Name : null,
                    pm.SequenceOrder,
                    pm.Status
                })
                .ToListAsync();

            var milestonesByProjectId = milestoneRows
                .GroupBy(m => m.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(m => new ViewModels.DepartmentHead.ProjectMilestoneViewModel
                {
                    Id = m.Id,
                    ProjectMilestoneId = m.Id,
                    MilestoneName = m.MilestoneName ?? string.Empty,
                    SequenceOrder = m.SequenceOrder,
                    Status = m.Status ?? string.Empty
                }).ToList());

            var projects = projectRows.Select(p => new DepartmentHeadProjectListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                ProposalTitle = p.ProposalTitle,
                ProjectManagerName = BuildFullName(p.ProjectManagerFirstName, p.ProjectManagerMiddleName, p.ProjectManagerLastName),
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                DateCreated = p.DateCreated,
                MemberCount = memberCountByProjectId.TryGetValue(p.Id, out var count) ? count : 0,
                Members = membersByProjectId.TryGetValue(p.Id, out var mems) ? mems : new List<ViewModels.DepartmentHead.MemberViewModel>(),
                Milestones = milestonesByProjectId.TryGetValue(p.Id, out var ms) ? ms : new List<ViewModels.DepartmentHead.ProjectMilestoneViewModel>(),
                Status = (p.EndDate.Date < DateTime.Today) ? "Finished" : "Unfinished",
                IsArchived = p.IsArchived
            }).ToList();

            var usedProposalIds = await _context.Projects.Select(p => p.ProjectProposalId).ToListAsync();

            var proposals = await _context.ProjectProposals
                .Include(p => p.Employee)
                .Where(p => p.Status == "Approved"
                    && p.Employee != null
                    && p.Employee.DepartmentId == dh.DepartmentId
                    && !usedProposalIds.Contains(p.Id))
                .OrderByDescending(p => p.DateCreated)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Title
                })
                .ToListAsync();

            var projectManagerRows = await _context.ProjectManagers
                .OrderBy(pm => pm.LastName)
                .ThenBy(pm => pm.FirstName)
                .Select(pm => new
                {
                    pm.Id,
                    pm.FirstName,
                    pm.MiddleName,
                    pm.LastName,
                    pm.DepartmentId,
                    DeptName = pm.Department != null ? pm.Department.Name : ""
                })
                .ToListAsync();

            var projectManagers = projectManagerRows
                .Select(pm => new SelectListItem
                {
                    Value = pm.Id.ToString(),
                    Text = BuildFullName(pm.FirstName, pm.MiddleName, pm.LastName)
                })
                .ToList();

            var availableProjectManagersPicker = projectManagerRows
                .Select(pm => new PmPickerItem
                {
                    Id = pm.Id,
                    Name = BuildFullName(pm.FirstName, pm.MiddleName, pm.LastName),
                    DeptId = pm.DepartmentId,
                    DeptName = pm.DeptName
                })
                .ToList();

            var employeeRows = await _context.Employees
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Select(e => new
                {
                    e.Id,
                    e.FirstName,
                    e.MiddleName,
                    e.LastName
                })
                .ToListAsync();

            var employees = employeeRows
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = BuildFullName(e.FirstName, e.MiddleName, e.LastName)
                })
                .ToList();

            var roles = await _context.ProjectRoles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                })
                .ToListAsync();

            return new DepartmentHeadProjectIndexViewModel
            {
                Projects = projects,
                CreateProject = createModel ?? new CreateDepartmentHeadProjectViewModel(),
                AvailableProposals = proposals,
                AvailableProjectManagers = projectManagers,
                AvailableProjectManagersPicker = availableProjectManagersPicker,
                AvailableEmployees = employees,
                AvailableProjectRoles = roles
            };
        }

        private async Task<DepartmentHeadProjectEditViewModel> BuildEditViewModelAsync(DepartmentHead dh, int projectId, EditDepartmentHeadProjectViewModel? editModel = null)
        {
            var usedProposalIds = await _context.Projects
                .Where(p => p.Id != projectId)
                .Select(p => p.ProjectProposalId)
                .ToListAsync();

            var proposals = await _context.ProjectProposals
                .Include(p => p.Employee)
                .Where(p => p.Status == "Approved"
                    && p.Employee != null
                    && p.Employee.DepartmentId == dh.DepartmentId
                    && !usedProposalIds.Contains(p.Id))
                .OrderByDescending(p => p.DateCreated)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Title
                })
                .ToListAsync();

            var projectManagerRows = await _context.ProjectManagers
                .OrderBy(pm => pm.LastName)
                .ThenBy(pm => pm.FirstName)
                .Select(pm => new
                {
                    pm.Id,
                    pm.FirstName,
                    pm.MiddleName,
                    pm.LastName,
                    pm.DepartmentId,
                    DeptName = pm.Department != null ? pm.Department.Name : ""
                })
                .ToListAsync();

            var projectManagers = projectManagerRows
                .Select(pm => new SelectListItem
                {
                    Value = pm.Id.ToString(),
                    Text = BuildFullName(pm.FirstName, pm.MiddleName, pm.LastName)
                })
                .ToList();

            var availableProjectManagersPicker = projectManagerRows
                .Select(pm => new PmPickerItem
                {
                    Id = pm.Id,
                    Name = BuildFullName(pm.FirstName, pm.MiddleName, pm.LastName),
                    DeptId = pm.DepartmentId,
                    DeptName = pm.DeptName
                })
                .ToList();

            var employeeRows = await _context.Employees
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Select(e => new
                {
                    e.Id,
                    e.FirstName,
                    e.MiddleName,
                    e.LastName
                })
                .ToListAsync();

            var employees = employeeRows
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = BuildFullName(e.FirstName, e.MiddleName, e.LastName)
                })
                .ToList();

            var roles = await _context.ProjectRoles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                })
                .ToListAsync();

            return new DepartmentHeadProjectEditViewModel
            {
                EditProject = editModel ?? new EditDepartmentHeadProjectViewModel(),
                AvailableProposals = proposals,
                AvailableProjectManagers = projectManagers,
                AvailableProjectManagersPicker = availableProjectManagersPicker,
                AvailableEmployees = employees,
                AvailableProjectRoles = roles
            };
        }

        private static string BuildFullName(string? firstName, string? middleName, string? lastName)
        {
            var parts = new[] { firstName, middleName, lastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim());

            var fullName = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(fullName) ? "N/A" : fullName;
        }
    }
}
