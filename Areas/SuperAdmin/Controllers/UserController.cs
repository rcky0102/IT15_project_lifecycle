using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Constants;
using project_lifecycle.Data;
using project_lifecycle.Models;
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

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
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

            viewModel.Users = userDetails;
            
            // Prepare create user form
            var departments = await _context.Departments.ToListAsync();
            var positions = await _context.Positions.ToListAsync();
            
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
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdown data
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

            // Check if email already exists
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

            // Add role to user
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            // Create employee record if role is Employee
            if (model.Role == Roles.Employee.ToString())
            {
                var employee = new Employee
                {
                    UserId = user.Id,
                    EmployeeNumber = model.EmployeeNumber,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    DepartmentId = model.DepartmentId,
                    PositionId = model.PositionId,
                    DateHired = model.DateHired
                };

                _context.Employees.Add(employee);
            }
            // Create human resource record if role is HumanResource
            else if (model.Role == Roles.HumanResource.ToString())
            {
                var humanResource = new HumanResource
                {
                    UserId = user.Id,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    Contact = model.Contact ?? "",
                    PositionId = model.PositionId
                };

                _context.HumanResources.Add(humanResource);
            }
            // Create department head record if role is DepartmentHead
            else if (model.Role == Roles.DepartmentHead.ToString())
            {
                var departmentHead = new DepartmentHead
                {
                    UserId = user.Id,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    Contact = model.Contact ?? "",
                    DepartmentId = model.DepartmentId,
                    PositionId = model.PositionId
                };

                _context.DepartmentHeads.Add(departmentHead);
            }
            // Create executive record if role is Executive
            else if (model.Role == Roles.Executive.ToString())
            {
                var executive = new Executive
                {
                    UserId = user.Id,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    Contact = model.Contact ?? "",
                    PositionId = model.PositionId
                };

                _context.Executives.Add(executive);
            }
            // Create project manager record if role is ProjectManager
            else if (model.Role == Roles.ProjectManager.ToString())
            {
                var projectManager = new ProjectManager
                {
                    UserId = user.Id,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    Contact = model.Contact ?? "",
                    DepartmentId = model.DepartmentId,
                    PositionId = model.PositionId
                };

                _context.ProjectManagers.Add(projectManager);
            }

            if (model.Role == Roles.Employee.ToString() || 
                model.Role == Roles.HumanResource.ToString() || 
                model.Role == Roles.DepartmentHead.ToString() || 
                model.Role == Roles.Executive.ToString() || 
                model.Role == Roles.ProjectManager.ToString())
            {
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "User created successfully!" });
            }

            TempData["Success"] = "User created successfully!";
            return RedirectToAction(nameof(Index));
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
    }
}
