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

                    // Get employee details if exists
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

                    userDetails.Add(userDetail);
                }
            }

            viewModel.Users = userDetails;
            
            // Prepare create user form
            viewModel.CreateUserViewModel = new CreateUserViewModel
            {
                Departments = await _context.Departments.ToListAsync(),
                Positions = await _context.Positions.ToListAsync()
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
                await _context.SaveChangesAsync();
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

                    userDetails.Add(userDetail);
                }
            }

            return userDetails;
        }
    }
}
