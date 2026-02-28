using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Constants;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;
using project_lifecycle.ViewModels;

namespace project_lifecycle.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "HumanResource")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _audit;

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditLogService audit)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
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
                            userDetail.DepartmentName = employee.Department?.Name;
                            userDetail.PositionName = employee.Position?.Name;
                            userDetail.DateHired = employee.DateHired;
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
                        }
                    }

                    userDetails.Add(userDetail);
                }
            }

            var departments = await _context.Departments.ToListAsync();
            var positions = await _context.Positions.ToListAsync();

            var model = new UserListViewModel
            {
                Users = userDetails,
                CreateUserViewModel = new CreateUserViewModel
                {
                    Departments = departments,
                    Positions = positions
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                ModelState.Remove("UserId");
                ModelState.Remove("ConfirmPassword");

                if (!ModelState.IsValid)
                {
                    model.Departments = await _context.Departments.ToListAsync();
                    model.Positions = await _context.Positions.ToListAsync();

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

                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    model.Departments = await _context.Departments.ToListAsync();
                    model.Positions = await _context.Positions.ToListAsync();

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

                    model.Departments = await _context.Departments.ToListAsync();
                    model.Positions = await _context.Positions.ToListAsync();

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

                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                await _userManager.AddToRoleAsync(user, model.Role);

                if (model.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    var employee = new global::project_lifecycle.Models.Employee
                    {
                        UserId = user.Id,
                        EmployeeNumber = model.EmployeeNumber,
                        FirstName = model.FirstName,
                        MiddleName = model.MiddleName,
                        LastName = model.LastName,
                        DepartmentId = model.DepartmentId ?? 0,
                        PositionId = model.PositionId ?? 0,
                        DateHired = model.DateHired
                    };

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
                        var executive = new Executive
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
                        var departmentHead = new DepartmentHead
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
                        var projectManager = new ProjectManager
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

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "User created successfully!" });
                }

                TempData["Success"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage = $"{ex.Message} - {ex.InnerException.Message}";
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage = $"{errorMessage} - {ex.InnerException.InnerException.Message}";
                    }
                }

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

                ModelState.AddModelError("", $"An error occurred: {errorMessage}");
                model.Departments = await _context.Departments.ToListAsync();
                model.Positions = await _context.Positions.ToListAsync();

                var vm = new UserListViewModel
                {
                    CreateUserViewModel = model,
                    Users = await GetUserListAsync()
                };

                return View("Index", vm);
            }
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
                            userDetail.DepartmentName = employee.Department?.Name;
                            userDetail.PositionName = employee.Position?.Name;
                            userDetail.DateHired = employee.DateHired;
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
                        }
                    }

                    userDetails.Add(userDetail);
                }
            }

            return userDetails;
        }

        // POST: /HumanResource/User/Delete
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

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains(Roles.Employee.ToString()))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == id);
                    if (employee != null) _context.Employees.Remove(employee);
                }
                else if (roles.Contains(Roles.HumanResource.ToString()))
                {
                    var humanResource = await _context.HumanResources.FirstOrDefaultAsync(hr => hr.UserId == id);
                    if (humanResource != null) _context.HumanResources.Remove(humanResource);
                }
                else if (roles.Contains(Roles.DepartmentHead.ToString()))
                {
                    var departmentHead = await _context.DepartmentHeads.FirstOrDefaultAsync(dh => dh.UserId == id);
                    if (departmentHead != null) _context.DepartmentHeads.Remove(departmentHead);
                }
                else if (roles.Contains(Roles.Executive.ToString()))
                {
                    var executive = await _context.Executives.FirstOrDefaultAsync(e => e.UserId == id);
                    if (executive != null) _context.Executives.Remove(executive);
                }
                else if (roles.Contains(Roles.ProjectManager.ToString()))
                {
                    var projectManager = await _context.ProjectManagers.FirstOrDefaultAsync(pm => pm.UserId == id);
                    if (projectManager != null) _context.ProjectManagers.Remove(projectManager);
                }

                await _context.SaveChangesAsync();

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

                await _audit.LogAsync(User, "Delete", "User Management", $"Deleted user '{user.Email}'", "User", id);

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

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An error occurred while deleting the user. Please try again." });
                }

                TempData["Error"] = "An error occurred while deleting the user. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /HumanResource/User/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(CreateUserViewModel model)
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

                if (!string.IsNullOrEmpty(model.Email) && !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    await _userManager.UpdateAsync(user);
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
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
                    employee.DepartmentId = model.DepartmentId ?? 0;
                    employee.PositionId = model.PositionId ?? 0;
                    employee.DateHired = model.DateHired;
                }
                else
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
                    if (employee != null) _context.Employees.Remove(employee);
                }

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
                }
                else
                {
                    var hr = await _context.HumanResources.FirstOrDefaultAsync(h => h.UserId == user.Id);
                    if (hr != null) _context.HumanResources.Remove(hr);
                }

                if (model.Role.Equals("DepartmentHead", StringComparison.OrdinalIgnoreCase))
                {
                    var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (dh == null)
                    {
                        dh = new DepartmentHead { UserId = user.Id };
                        _context.DepartmentHeads.Add(dh);
                    }
                    dh.EmployeeNumber = model.EmployeeNumber;
                    dh.FirstName = model.FirstName;
                    dh.MiddleName = model.MiddleName;
                    dh.LastName = model.LastName;
                    dh.Contact = model.Contact ?? string.Empty;
                    dh.DepartmentId = model.DepartmentId ?? 0;
                    dh.PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null;
                }
                else
                {
                    var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (dh != null) _context.DepartmentHeads.Remove(dh);
                }

                if (model.Role.Equals("Executive", StringComparison.OrdinalIgnoreCase))
                {
                    var ex = await _context.Executives.FirstOrDefaultAsync(e => e.UserId == user.Id);
                    if (ex == null)
                    {
                        ex = new Executive { UserId = user.Id };
                        _context.Executives.Add(ex);
                    }
                    ex.EmployeeNumber = model.EmployeeNumber;
                    ex.FirstName = model.FirstName;
                    ex.MiddleName = model.MiddleName;
                    ex.LastName = model.LastName;
                    ex.Contact = model.Contact ?? string.Empty;
                    ex.DepartmentId = model.DepartmentId.HasValue && model.DepartmentId.Value > 0 ? model.DepartmentId.Value : (int?)null;
                    ex.PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null;
                }
                else
                {
                    var ex = await _context.Executives.FirstOrDefaultAsync(e => e.UserId == user.Id);
                    if (ex != null) _context.Executives.Remove(ex);
                }

                if (model.Role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
                {
                    var pm = await _context.ProjectManagers.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (pm == null)
                    {
                        pm = new ProjectManager { UserId = user.Id };
                        _context.ProjectManagers.Add(pm);
                    }
                    pm.EmployeeNumber = model.EmployeeNumber;
                    pm.FirstName = model.FirstName;
                    pm.MiddleName = model.MiddleName;
                    pm.LastName = model.LastName;
                    pm.Contact = model.Contact ?? string.Empty;
                    pm.DepartmentId = model.DepartmentId ?? 0;
                    pm.PositionId = model.PositionId.HasValue && model.PositionId.Value > 0 ? model.PositionId.Value : (int?)null;
                }
                else
                {
                    var pm = await _context.ProjectManagers.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (pm != null) _context.ProjectManagers.Remove(pm);
                }

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

                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            var errors = ModelState.ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                            );
                            return Json(new { success = false, errors = errors });
                        }

                        model.Departments = await _context.Departments.ToListAsync();
                        model.Positions = await _context.Positions.ToListAsync();
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
