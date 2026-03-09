using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Constants;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;
using project_lifecycle.ViewModels;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _audit;
        private readonly INotificationService _notif;
        private readonly IWebHostEnvironment _env;

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditLogService audit,
            INotificationService notif,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _audit = audit;
            _notif = notif;
            _env = env;
        }

        // GET: /SuperAdmin/User/Index
        public async Task<IActionResult> Index()
        {
            var viewModel = new UserListViewModel();
            
            // Get all users with specific admin roles and Employee role
            var users = await _userManager.Users.ToListAsync();
            var userDetails = new List<UserDetailsViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(Roles.HumanResource.ToString()) || 
                    roles.Contains(Roles.DepartmentHead.ToString()) || 
                    roles.Contains(Roles.Executive.ToString()) || 
                    roles.Contains(Roles.ProjectManager.ToString()) || 
                    roles.Contains(Roles.Employee.ToString()))
                {
                    var userDetail = new UserDetailsViewModel
                    {
                        UserId = user.Id,
                        Email = user.Email ?? string.Empty,
                        Role = roles.FirstOrDefault() ?? "No Role"
                    };

                    // Get role-specific details
                    if (roles.Contains(Roles.Employee.ToString()))
                    {
                        var employee = await _context.Employees
                            .Include(e => e.Department)
                            .Include(e => e.Position)
                            .FirstOrDefaultAsync(e => e.UserId == user.Id);

                        if (employee != null)
                        {
                            userDetail.EmployeeNumber = employee.EmployeeNumber;
                            userDetail.FirstName = employee.FirstName;
                            userDetail.MiddleName = employee.MiddleName;
                            userDetail.LastName = employee.LastName;
                            userDetail.Contact = employee.Contact;
                            userDetail.DepartmentName = employee.Department?.Name;
                            userDetail.PositionName = employee.Position?.Name;
                            userDetail.DateHired = employee.DateHired;
                            userDetail.ProfileImage = employee.ProfileImage;
                        }
                    }
                    else if (roles.Contains(Roles.HumanResource.ToString()))
                    {
                        var humanResource = await _context.HumanResources
                            .Include(hr => hr.Department)
                            .Include(hr => hr.Position)
                            .FirstOrDefaultAsync(hr => hr.UserId == user.Id);

                        if (humanResource != null)
                        {
                            userDetail.EmployeeNumber = humanResource.EmployeeNumber;
                            userDetail.FirstName = humanResource.FirstName;
                            userDetail.MiddleName = humanResource.MiddleName;
                            userDetail.LastName = humanResource.LastName;
                            userDetail.DepartmentName = humanResource.Department?.Name;
                            userDetail.PositionName = humanResource.Position?.Name;
                            userDetail.DateHired = humanResource.CreatedDate;
                            userDetail.ProfileImage = humanResource.ProfileImage;
                        }
                    }
                    else if (roles.Contains(Roles.DepartmentHead.ToString()))
                    {
                        var departmentHead = await _context.DepartmentHeads
                            .Include(dh => dh.Department)
                            .Include(dh => dh.Position)
                            .FirstOrDefaultAsync(dh => dh.UserId == user.Id);

                        if (departmentHead != null)
                        {
                            userDetail.EmployeeNumber = departmentHead.EmployeeNumber;
                            userDetail.FirstName = departmentHead.FirstName;
                            userDetail.MiddleName = departmentHead.MiddleName;
                            userDetail.LastName = departmentHead.LastName;
                            userDetail.DepartmentName = departmentHead.Department?.Name;
                            userDetail.PositionName = departmentHead.Position?.Name;
                            userDetail.DateHired = departmentHead.CreatedDate;
                            userDetail.ProfileImage = departmentHead.ProfileImage;
                        }
                    }
                    else if (roles.Contains(Roles.Executive.ToString()))
                    {
                        var executive = await _context.Executives
                            .Include(e => e.Department)
                            .Include(e => e.Position)
                            .FirstOrDefaultAsync(e => e.UserId == user.Id);

                        if (executive != null)
                        {
                            userDetail.EmployeeNumber = executive.EmployeeNumber;
                            userDetail.FirstName = executive.FirstName;
                            userDetail.MiddleName = executive.MiddleName;
                            userDetail.LastName = executive.LastName;
                            userDetail.DepartmentName = executive.Department?.Name;
                            userDetail.PositionName = executive.Position?.Name;
                            userDetail.DateHired = executive.CreatedDate;
                            userDetail.ProfileImage = executive.ProfileImage;
                        }
                    }
                    else if (roles.Contains(Roles.ProjectManager.ToString()))
                    {
                        var projectManager = await _context.ProjectManagers
                            .Include(pm => pm.Department)
                            .Include(pm => pm.Position)
                            .FirstOrDefaultAsync(pm => pm.UserId == user.Id);

                        if (projectManager != null)
                        {
                            userDetail.EmployeeNumber = projectManager.EmployeeNumber;
                            userDetail.FirstName = projectManager.FirstName;
                            userDetail.MiddleName = projectManager.MiddleName;
                            userDetail.LastName = projectManager.LastName;
                            userDetail.DepartmentName = projectManager.Department?.Name;
                            userDetail.PositionName = projectManager.Position?.Name;
                            userDetail.DateHired = projectManager.CreatedDate;
                            userDetail.ProfileImage = projectManager.ProfileImage;
                        }
                    }

                    userDetails.Add(userDetail);
                }
            }

            viewModel.Users = userDetails;
            
            // Prepare create user form
            var departments = await _context.Departments.Where(d => !d.IsArchived).ToListAsync();
            var positions = await _context.Positions.Where(p => !p.IsArchived).ToListAsync();
            
            // Debug: Log counts
            Console.WriteLine($"Departments count: {departments.Count}");
            Console.WriteLine($"Positions count: {positions.Count}");
            
            viewModel.CreateUserViewModel = new CreateUserViewModel
            {
                Departments = departments,
                Positions = positions
            };

            return View(viewModel);
        }

        // POST: /SuperAdmin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] CreateUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload dropdown data
                    model.Departments = await _context.Departments.Where(d => !d.IsArchived).ToListAsync();
                    model.Positions = await _context.Positions.Where(p => !p.IsArchived).ToListAsync();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        var errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                        return Json(new { success = false, errors = errors });
                    }
                    
                    var viewModel = new UserListViewModel
                    {
                        CreateUserViewModel = model,
                        Users = await GetUserListAsync()
                    };
                    
                    return View("Index", viewModel);
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    model.Departments = await _context.Departments.Where(d => !d.IsArchived).ToListAsync();
                    model.Positions = await _context.Positions.Where(p => !p.IsArchived).ToListAsync();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        var errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                        return Json(new { success = false, errors = errors });
                    }
                    
                    var viewModel = new UserListViewModel
                    {
                        CreateUserViewModel = model,
                        Users = await GetUserListAsync()
                    };
                    
                    return View("Index", viewModel);
                }

                // Create user
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    
                    model.Departments = await _context.Departments.Where(d => !d.IsArchived).ToListAsync();
                    model.Positions = await _context.Positions.Where(p => !p.IsArchived).ToListAsync();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        var errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                        return Json(new { success = false, errors = errors });
                    }
                    
                    var viewModel = new UserListViewModel
                    {
                        CreateUserViewModel = model,
                        Users = await GetUserListAsync()
                    };
                    
                    return View("Index", viewModel);
                }

                // Add role to user
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                await _userManager.AddToRoleAsync(user, model.Role);

                // Create role-specific record using more robust comparison
                if (model.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    var employee = new global::project_lifecycle.Models.Employee
                    {
                        UserId = user.Id,
                        EmployeeNumber = model.EmployeeNumber,
                        FirstName = model.FirstName,
                        MiddleName = model.MiddleName,
                        LastName = model.LastName,
                        Contact = model.Contact ?? "",
                        DepartmentId = model.DepartmentId ?? 0,
                        PositionId = model.PositionId ?? 0,
                        DateHired = model.DateHired
                    };

                    if (model.ProfileImageFile != null)
                    {
                        employee.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                    }

                    _context.Employees.Add(employee);
                }
                else if (model.Role.Equals("HumanResource", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var humanResource = new global::project_lifecycle.Models.HumanResource
                        {
                            UserId = user.Id,
                            EmployeeNumber = model.EmployeeNumber,
                            FirstName = model.FirstName,
                            MiddleName = model.MiddleName,
                            LastName = model.LastName,
                            Contact = model.Contact ?? "",
                            PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null
                        };

                        if (model.ProfileImageFile != null)
                        {
                            humanResource.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                        }

                        _context.HumanResources.Add(humanResource);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error creating human resource: {ex.Message}");
                    }
                }
                else if (model.Role.Equals("Executive", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var executive = new global::project_lifecycle.Models.Executive
                        {
                            UserId = user.Id,
                            EmployeeNumber = model.EmployeeNumber,
                            FirstName = model.FirstName,
                            MiddleName = model.MiddleName,
                            LastName = model.LastName,
                            Contact = model.Contact ?? "",
                            DepartmentId = (model.DepartmentId.HasValue && model.DepartmentId.Value > 0) ? model.DepartmentId.Value : (int?)null,
                            PositionId = (model.PositionId.HasValue && model.PositionId.Value > 0) ? model.PositionId.Value : (int?)null
                        };

                        if (model.ProfileImageFile != null)
                        {
                            executive.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                        }

                        _context.Executives.Add(executive);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error creating executive: {ex.Message}");
                    }
                }
                else if (model.Role.Equals("DepartmentHead", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var departmentHead = new global::project_lifecycle.Models.DepartmentHead
                        {
                            UserId = user.Id,
                            EmployeeNumber = model.EmployeeNumber,
                            FirstName = model.FirstName,
                            MiddleName = model.MiddleName,
                            LastName = model.LastName,
                            Contact = model.Contact ?? "",
                            DepartmentId = (model.DepartmentId.HasValue && model.DepartmentId.Value > 0) ? model.DepartmentId.Value : 0,
                            PositionId = (model.PositionId.HasValue && model.PositionId.Value > 0) ? model.PositionId.Value : (int?)null
                        };

                        if (model.ProfileImageFile != null)
                        {
                            departmentHead.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                        }

                        _context.DepartmentHeads.Add(departmentHead);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error creating department head: {ex.Message}");
                    }
                }
                else if (model.Role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var projectManager = new global::project_lifecycle.Models.ProjectManager
                        {
                            UserId = user.Id,
                            EmployeeNumber = model.EmployeeNumber,
                            FirstName = model.FirstName,
                            MiddleName = model.MiddleName,
                            LastName = model.LastName,
                            Contact = model.Contact ?? "",
                            DepartmentId = (model.DepartmentId.HasValue && model.DepartmentId.Value > 0) ? model.DepartmentId.Value : 0,
                            PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null
                        };

                        if (model.ProfileImageFile != null)
                        {
                            projectManager.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                        }

                        _context.ProjectManagers.Add(projectManager);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error creating project manager: {ex.Message}");
                    }
                }

                if (model.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase) || 
                    model.Role.Equals("HumanResource", StringComparison.OrdinalIgnoreCase) || 
                    model.Role.Equals("DepartmentHead", StringComparison.OrdinalIgnoreCase) || 
                    model.Role.Equals("Executive", StringComparison.OrdinalIgnoreCase) || 
                    model.Role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
                {
                    await _context.SaveChangesAsync();
                }

                await _audit.LogAsync(User, "Create", "User Management", $"Created user '{model.FirstName} {model.LastName}' ({model.Email}) with role {model.Role}", "User", user.Id);

                var dashboardLink = "/";
                if (!string.IsNullOrEmpty(model.Role))
                {
                    dashboardLink = $"/{model.Role}";
                }

                await _notif.CreateAsync(
                    recipientId: user.Id,
                    title: "Welcome!",
                    message: $"Your account has been created with the role {model.Role}.",
                    type: "Success",
                    link: dashboardLink,
                    module: "User Management"
                );

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "User created successfully!" });
                }

                TempData["Success"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Error creating user: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                // Build detailed error message
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage = $"{ex.Message} - {ex.InnerException.Message}";
                    // Check for database errors
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage = $"{errorMessage} - {ex.InnerException.InnerException.Message}";
                    }
                }

                // Handle AJAX requests with error response
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errorDict = new Dictionary<string, string[]>
                    {
                        { "General", new[] { errorMessage } }
                    };
                    return Json(new { 
                        success = false, 
                        message = "Error creating user",
                        detailedMessage = errorMessage,
                        errors = errorDict
                    });
                }

                // For non-AJAX requests, add error to ModelState and return the view
                ModelState.AddModelError("", $"An error occurred: {errorMessage}");
                model.Departments = await _context.Departments.Where(d => !d.IsArchived).ToListAsync();
                model.Positions = await _context.Positions.Where(p => !p.IsArchived).ToListAsync();
                
                var viewModel = new UserListViewModel
                {
                    CreateUserViewModel = model,
                    Users = await GetUserListAsync()
                };
                
                return View("Index", viewModel);
            }
        }

        private async Task<string> SaveProfileImageAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/profiles/{fileName}";
        }

        private async Task<List<UserDetailsViewModel>> GetUserListAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDetails = new List<UserDetailsViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(Roles.HumanResource.ToString()) || 
                    roles.Contains(Roles.DepartmentHead.ToString()) || 
                    roles.Contains(Roles.Executive.ToString()) || 
                    roles.Contains(Roles.ProjectManager.ToString()) || 
                    roles.Contains(Roles.Employee.ToString()))
                {
                    var userDetail = new UserDetailsViewModel
                    {
                        UserId = user.Id,
                        Email = user.Email ?? string.Empty,
                        Role = roles.FirstOrDefault() ?? "No Role"
                    };

                    // Get role-specific details
                    if (roles.Contains(Roles.Employee.ToString()))
                    {
                        var employee = await _context.Employees
                            .Include(e => e.Department)
                            .Include(e => e.Position)
                            .FirstOrDefaultAsync(e => e.UserId == user.Id);

                        if (employee != null)
                        {
                            userDetail.EmployeeNumber = employee.EmployeeNumber;
                            userDetail.FirstName = employee.FirstName;
                            userDetail.MiddleName = employee.MiddleName;
                            userDetail.LastName = employee.LastName;
                            userDetail.Contact = employee.Contact;
                            userDetail.DepartmentName = employee.Department?.Name;
                            userDetail.PositionName = employee.Position?.Name;
                            userDetail.DateHired = employee.DateHired;
                            userDetail.ProfileImage = employee.ProfileImage;
                        }
                    }
                    else if (roles.Contains(Roles.HumanResource.ToString()))
                    {
                        var humanResource = await _context.HumanResources
                            .Include(hr => hr.Position)
                            .FirstOrDefaultAsync(hr => hr.UserId == user.Id);

                        if (humanResource != null)
                        {
                            userDetail.FirstName = humanResource.FirstName;
                            userDetail.MiddleName = humanResource.MiddleName;
                            userDetail.LastName = humanResource.LastName;
                            userDetail.PositionName = humanResource.Position?.Name;
                            userDetail.ProfileImage = humanResource.ProfileImage;
                        }
                    }
                    else if (roles.Contains(Roles.DepartmentHead.ToString()))
                    {
                        var departmentHead = await _context.DepartmentHeads
                            .Include(dh => dh.Department)
                            .Include(dh => dh.Position)
                            .FirstOrDefaultAsync(dh => dh.UserId == user.Id);

                        if (departmentHead != null)
                        {
                            userDetail.FirstName = departmentHead.FirstName;
                            userDetail.MiddleName = departmentHead.MiddleName;
                            userDetail.LastName = departmentHead.LastName;
                            userDetail.DepartmentName = departmentHead.Department?.Name;
                            userDetail.PositionName = departmentHead.Position?.Name;
                            userDetail.ProfileImage = departmentHead.ProfileImage;
                        }
                    }
                    else if (roles.Contains(Roles.Executive.ToString()))
                    {
                        var executive = await _context.Executives
                            .Include(e => e.Position)
                            .FirstOrDefaultAsync(e => e.UserId == user.Id);

                        if (executive != null)
                        {
                            userDetail.FirstName = executive.FirstName;
                            userDetail.MiddleName = executive.MiddleName;
                            userDetail.LastName = executive.LastName;
                            userDetail.PositionName = executive.Position?.Name;
                            userDetail.ProfileImage = executive.ProfileImage;
                        }
                    }
                    else if (roles.Contains(Roles.ProjectManager.ToString()))
                    {
                        var projectManager = await _context.ProjectManagers
                            .Include(pm => pm.Department)
                            .Include(pm => pm.Position)
                            .FirstOrDefaultAsync(pm => pm.UserId == user.Id);

                        if (projectManager != null)
                        {
                            userDetail.FirstName = projectManager.FirstName;
                            userDetail.MiddleName = projectManager.MiddleName;
                            userDetail.LastName = projectManager.LastName;
                            userDetail.DepartmentName = projectManager.Department?.Name;
                            userDetail.PositionName = projectManager.Position?.Name;
                            userDetail.ProfileImage = projectManager.ProfileImage;
                        }
                    }

                    userDetails.Add(userDetail);
                }
            }

            return userDetails;
        }

        // POST: /SuperAdmin/User/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "User ID is required" });
                    }
                    TempData["Error"] = "User ID is required";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "User not found" });
                    }
                    TempData["Error"] = "User not found";
                    return RedirectToAction(nameof(Index));
                }

                var userEmail = user.Email;
                var roles = await _userManager.GetRolesAsync(user);

                // ── 1. Clean up records that reference IdentityUser (UserId/SenderId) ──
                var chatMessages = await _context.ChatMessages.Where(m => m.SenderId == id).ToListAsync();
                _context.ChatMessages.RemoveRange(chatMessages);

                var convParticipants = await _context.ConversationParticipants.Where(p => p.UserId == id).ToListAsync();
                _context.ConversationParticipants.RemoveRange(convParticipants);

                var notifications = await _context.Notifications.Where(n => n.RecipientId == id).ToListAsync();
                _context.Notifications.RemoveRange(notifications);

                var auditLogs = await _context.AuditLogs.Where(a => a.UserId == id).ToListAsync();
                _context.AuditLogs.RemoveRange(auditLogs);

                // ── 2. Clean up role-specific child records, then remove the role record ──
                if (roles.Contains(Roles.Employee.ToString()))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == id);
                    if (employee != null)
                    {
                        var empId = employee.Id;

                        // Document children (versions & collaborators reference EmployeeId)
                        var docVersions = await _context.DocumentVersions.Where(v => v.EmployeeId == empId).ToListAsync();
                        _context.DocumentVersions.RemoveRange(docVersions);

                        var docCollabs = await _context.DocumentCollaborators.Where(c => c.EmployeeId == empId).ToListAsync();
                        _context.DocumentCollaborators.RemoveRange(docCollabs);

                        var docs = await _context.Documents.Where(d => d.OwnerEmployeeId == empId).ToListAsync();
                        foreach (var doc in docs)
                        {
                            var childVersions = await _context.DocumentVersions.Where(v => v.DocumentId == doc.Id).ToListAsync();
                            _context.DocumentVersions.RemoveRange(childVersions);
                            var childCollabs = await _context.DocumentCollaborators.Where(c => c.DocumentId == doc.Id).ToListAsync();
                            _context.DocumentCollaborators.RemoveRange(childCollabs);
                        }
                        _context.Documents.RemoveRange(docs);

                        // Proposal children
                        var proposalVersions = await _context.ProjectProposalVersions.Where(v => v.EmployeeId == empId).ToListAsync();
                        _context.ProjectProposalVersions.RemoveRange(proposalVersions);

                        var proposals = await _context.ProjectProposals.Where(p => p.EmployeeId == empId).ToListAsync();
                        foreach (var prop in proposals)
                        {
                            var propVersions = await _context.ProjectProposalVersions.Where(v => v.ProjectProposalId == prop.Id).ToListAsync();
                            _context.ProjectProposalVersions.RemoveRange(propVersions);
                            var propNotes = await _context.ProposalNoteVersions.Where(n => n.ProjectProposalId == prop.Id).ToListAsync();
                            _context.ProposalNoteVersions.RemoveRange(propNotes);
                        }
                        _context.ProjectProposals.RemoveRange(proposals);

                        // Task members → members
                        var members = await _context.Members.Where(m => m.EmployeeId == empId).ToListAsync();
                        foreach (var member in members)
                        {
                            var taskMembers = await _context.TaskMembers.Where(t => t.MemberId == member.Id).ToListAsync();
                            _context.TaskMembers.RemoveRange(taskMembers);
                        }
                        _context.Members.RemoveRange(members);

                        _context.Employees.Remove(employee);
                    }
                }
                else if (roles.Contains(Roles.ProjectManager.ToString()))
                {
                    var pm = await _context.ProjectManagers.FirstOrDefaultAsync(e => e.UserId == id);
                    if (pm != null)
                    {
                        var pmId = pm.Id;

                        // Task note versions
                        var taskNotes = await _context.TaskNoteVersions.Where(v => v.ProjectManagerId == pmId).ToListAsync();
                        _context.TaskNoteVersions.RemoveRange(taskNotes);

                        // Project tasks owned by this PM
                        var tasks = await _context.ProjectTasks.Where(t => t.ProjectManagerId == pmId).ToListAsync();
                        foreach (var task in tasks)
                        {
                            var tm = await _context.TaskMembers.Where(t => t.ProjectTaskId == task.Id).ToListAsync();
                            _context.TaskMembers.RemoveRange(tm);
                            var tv = await _context.ProjectTaskVersions.Where(v => v.ProjectTaskId == task.Id).ToListAsync();
                            _context.ProjectTaskVersions.RemoveRange(tv);
                            var tn = await _context.TaskNoteVersions.Where(v => v.ProjectTaskId == task.Id).ToListAsync();
                            _context.TaskNoteVersions.RemoveRange(tn);
                        }
                        _context.ProjectTasks.RemoveRange(tasks);

                        // Projects owned by this PM
                        var projects = await _context.Projects.Where(p => p.ProjectManagerId == pmId).ToListAsync();
                        foreach (var proj in projects)
                        {
                            // Project milestones
                            var pms = await _context.ProjectMilestones.Where(m => m.ProjectId == proj.Id).ToListAsync();
                            _context.ProjectMilestones.RemoveRange(pms);

                            // Members & task members
                            var projMembers = await _context.Members.Where(m => m.ProjectId == proj.Id).ToListAsync();
                            foreach (var member in projMembers)
                            {
                                var tms = await _context.TaskMembers.Where(t => t.MemberId == member.Id).ToListAsync();
                                _context.TaskMembers.RemoveRange(tms);
                            }
                            _context.Members.RemoveRange(projMembers);

                            // Project tasks (via milestones)
                            var milestoneIds = pms.Select(m => m.Id).ToList();
                            var projTasks = await _context.ProjectTasks.Where(t => milestoneIds.Contains(t.ProjectMilestoneId)).ToListAsync();
                            foreach (var task in projTasks)
                            {
                                var tm = await _context.TaskMembers.Where(t => t.ProjectTaskId == task.Id).ToListAsync();
                                _context.TaskMembers.RemoveRange(tm);
                                var tv = await _context.ProjectTaskVersions.Where(v => v.ProjectTaskId == task.Id).ToListAsync();
                                _context.ProjectTaskVersions.RemoveRange(tv);
                                var tn = await _context.TaskNoteVersions.Where(v => v.ProjectTaskId == task.Id).ToListAsync();
                                _context.TaskNoteVersions.RemoveRange(tn);
                            }
                            _context.ProjectTasks.RemoveRange(projTasks);
                        }
                        _context.Projects.RemoveRange(projects);

                        _context.ProjectManagers.Remove(pm);
                    }
                }
                else if (roles.Contains(Roles.DepartmentHead.ToString()))
                {
                    var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == id);
                    if (dh != null)
                    {
                        var dhId = dh.Id;

                        // Proposal note versions
                        var propNotes = await _context.ProposalNoteVersions.Where(n => n.DepartmentHeadId == dhId).ToListAsync();
                        _context.ProposalNoteVersions.RemoveRange(propNotes);

                        // Nullify DepartmentHeadId on proposals (nullable FK)
                        var proposals = await _context.ProjectProposals.Where(p => p.DepartmentHeadId == dhId).ToListAsync();
                        foreach (var prop in proposals)
                            prop.DepartmentHeadId = null;

                        _context.DepartmentHeads.Remove(dh);
                    }
                }
                else if (roles.Contains(Roles.HumanResource.ToString()))
                {
                    var hr = await _context.HumanResources.FirstOrDefaultAsync(h => h.UserId == id);
                    if (hr != null) _context.HumanResources.Remove(hr);
                }
                else if (roles.Contains(Roles.Executive.ToString()))
                {
                    var ex = await _context.Executives.FirstOrDefaultAsync(e => e.UserId == id);
                    if (ex != null) _context.Executives.Remove(ex);
                }

                // ── 3. Persist all child-record removals ──
                await _context.SaveChangesAsync();

                // ── 4. Delete the Identity user ──
                var result = await _userManager.DeleteAsync(user);
                
                if (!result.Succeeded)
                {
                    var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = $"Failed to delete user: {errorMessages}" });
                    }
                    TempData["Error"] = $"Failed to delete user: {errorMessages}";
                    return RedirectToAction(nameof(Index));
                }

                await _audit.LogAsync(User, "Delete", "User Management", $"Deleted user '{userEmail}'", "User", id);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "User deleted successfully!" });
                }

                TempData["Success"] = "User deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
                Console.WriteLine($"Inner: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"An error occurred: {ex.InnerException?.Message ?? ex.Message}" });
                }
                
                TempData["Error"] = "An error occurred while deleting the user. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /SuperAdmin/User/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] CreateUserViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserId))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "UserId is required" });
                    }
                    TempData["Error"] = "UserId is required";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "User not found" });
                    }
                    TempData["Error"] = "User not found";
                    return RedirectToAction(nameof(Index));
                }

                // Update email if changed
                if (!string.IsNullOrEmpty(model.Email) && !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    await _userManager.UpdateAsync(user);
                }

                // Update role if necessary
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
                    // Remove all and add the new role
                    if (currentRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    }
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // Update role-specific records: update if exists, create if missing
                // Employee
                if (model.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
                    if (employee == null)
                    {
                        employee = new global::project_lifecycle.Models.Employee { UserId = user.Id };
                        _context.Employees.Add(employee);
                    }

                    employee.EmployeeNumber = model.EmployeeNumber;
                    employee.FirstName = model.FirstName;
                    employee.MiddleName = model.MiddleName;
                    employee.LastName = model.LastName;
                    employee.Contact = model.Contact ?? string.Empty;
                    employee.DepartmentId = model.DepartmentId ?? 0;
                    employee.PositionId = model.PositionId ?? 0;
                    employee.DateHired = model.DateHired;

                    if (model.ProfileImageFile != null)
                    {
                        employee.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                    }
                }
                else
                {
                    // Remove employee record if role changed away from Employee
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
                    if (employee != null)
                    {
                        _context.Employees.Remove(employee);
                    }
                }

                // HumanResource
                if (model.Role.Equals("HumanResource", StringComparison.OrdinalIgnoreCase))
                {
                    var hr = await _context.HumanResources.FirstOrDefaultAsync(h => h.UserId == user.Id);
                    if (hr == null)
                    {
                        hr = new global::project_lifecycle.Models.HumanResource { UserId = user.Id };
                        _context.HumanResources.Add(hr);
                    }
                    hr.EmployeeNumber = model.EmployeeNumber;
                    hr.FirstName = model.FirstName;
                    hr.MiddleName = model.MiddleName;
                    hr.LastName = model.LastName;
                    hr.Contact = model.Contact ?? string.Empty;
                    hr.PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null;

                    if (model.ProfileImageFile != null)
                    {
                        hr.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                    }
                }
                else
                {
                    var hr = await _context.HumanResources.FirstOrDefaultAsync(h => h.UserId == user.Id);
                    if (hr != null)
                    {
                        _context.HumanResources.Remove(hr);
                    }
                }

                // DepartmentHead
                if (model.Role.Equals("DepartmentHead", StringComparison.OrdinalIgnoreCase))
                {
                    var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (dh == null)
                    {
                        dh = new global::project_lifecycle.Models.DepartmentHead { UserId = user.Id };
                        _context.DepartmentHeads.Add(dh);
                    }
                    dh.EmployeeNumber = model.EmployeeNumber;
                    dh.FirstName = model.FirstName;
                    dh.MiddleName = model.MiddleName;
                    dh.LastName = model.LastName;
                    dh.Contact = model.Contact ?? string.Empty;
                    dh.DepartmentId = model.DepartmentId ?? 0;
                    dh.PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null;

                    if (model.ProfileImageFile != null)
                    {
                        dh.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                    }
                }
                else
                {
                    var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (dh != null)
                    {
                        _context.DepartmentHeads.Remove(dh);
                    }
                }

                // Executive
                if (model.Role.Equals("Executive", StringComparison.OrdinalIgnoreCase))
                {
                    var ex = await _context.Executives.FirstOrDefaultAsync(e => e.UserId == user.Id);
                    if (ex == null)
                    {
                        ex = new global::project_lifecycle.Models.Executive { UserId = user.Id };
                        _context.Executives.Add(ex);
                    }
                    ex.EmployeeNumber = model.EmployeeNumber;
                    ex.FirstName = model.FirstName;
                    ex.MiddleName = model.MiddleName;
                    ex.LastName = model.LastName;
                    ex.Contact = model.Contact ?? string.Empty;
                    ex.DepartmentId = model.DepartmentId.HasValue && model.DepartmentId.Value > 0 ? model.DepartmentId.Value : (int?)null;
                    ex.PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null;

                    if (model.ProfileImageFile != null)
                    {
                        ex.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                    }
                }
                else
                {
                    var ex = await _context.Executives.FirstOrDefaultAsync(e => e.UserId == user.Id);
                    if (ex != null)
                    {
                        _context.Executives.Remove(ex);
                    }
                }

                // ProjectManager
                if (model.Role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
                {
                    var pm = await _context.ProjectManagers.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (pm == null)
                    {
                        pm = new global::project_lifecycle.Models.ProjectManager { UserId = user.Id };
                        _context.ProjectManagers.Add(pm);
                    }
                    pm.EmployeeNumber = model.EmployeeNumber;
                    pm.FirstName = model.FirstName;
                    pm.MiddleName = model.MiddleName;
                    pm.LastName = model.LastName;
                    pm.Contact = model.Contact ?? string.Empty;
                    pm.DepartmentId = model.DepartmentId ?? 0;
                    pm.PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null;

                    if (model.ProfileImageFile != null)
                    {
                        pm.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
                    }
                }
                else
                {
                    var pm = await _context.ProjectManagers.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (pm != null)
                    {
                        _context.ProjectManagers.Remove(pm);
                    }
                }

                // Update password if provided (admin reset)
                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var pwdResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!pwdResult.Succeeded)
                    {
                        foreach (var error in pwdResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        // Return validation errors for AJAX requests
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            var errors = ModelState.ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                            );
                            return Json(new { success = false, errors = errors });
                        }

                        // For non-AJAX, reload dropdowns and return to Index with model errors
                        model.Departments = await _context.Departments.Where(d => !d.IsArchived).ToListAsync();
                        model.Positions = await _context.Positions.Where(p => !p.IsArchived).ToListAsync();
                        var viewModel = new UserListViewModel
                        {
                            CreateUserViewModel = model,
                            Users = await GetUserListAsync()
                        };
                        return View("Index", viewModel);
                    }
                }

                await _context.SaveChangesAsync();

                await _audit.LogAsync(User, "Update", "User Management", $"Updated user '{model.FirstName} {model.LastName}' ({model.Email}) – role: {model.Role}", "User", model.UserId);

                var dashboardLink = "/";
                if (!string.IsNullOrEmpty(model.Role))
                {
                    dashboardLink = $"/{model.Role}";
                }

                await _notif.CreateAsync(
                    recipientId: model.UserId,
                    title: "Account Updated",
                    message: "Your account details have been updated by an administrator.",
                    type: "Info",
                    link: dashboardLink,
                    module: "User Management"
                );

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "User updated successfully!" });
                }

                TempData["Success"] = "User updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An error occurred while updating the user.", detailedMessage = ex.Message });
                }

                TempData["Error"] = "An error occurred while updating the user.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
