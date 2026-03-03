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
                .Where(pm => pm.DepartmentId == dh.DepartmentId)
                .Select(pm => pm.Id)
                .ToListAsync();

            if (!availableProjectManagerIds.Contains(createModel.ProjectManagerId))
            {
                ModelState.AddModelError("CreateProject.ProjectManagerId", "Please select a valid project manager.");
            }

            var availableEmployeeIds = await _context.Employees
                .Where(e => e.DepartmentId == dh.DepartmentId)
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
                ViewData["OpenCreateModal"] = true;
                var invalidModel = await BuildIndexViewModelAsync(dh, createModel);
                return View("Index", invalidModel);
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
                    DateCreated = p.DateCreated
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
                Status = (p.EndDate.Date < DateTime.Today) ? "Finished" : "Unfinished"
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
                .Where(pm => pm.DepartmentId == dh.DepartmentId)
                .OrderBy(pm => pm.LastName)
                .ThenBy(pm => pm.FirstName)
                .Select(pm => new
                {
                    pm.Id,
                    pm.FirstName,
                    pm.MiddleName,
                    pm.LastName
                })
                .ToListAsync();

            var projectManagers = projectManagerRows
                .Select(pm => new SelectListItem
                {
                    Value = pm.Id.ToString(),
                    Text = BuildFullName(pm.FirstName, pm.MiddleName, pm.LastName)
                })
                .ToList();

            var employeeRows = await _context.Employees
                .Where(e => e.DepartmentId == dh.DepartmentId)
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
