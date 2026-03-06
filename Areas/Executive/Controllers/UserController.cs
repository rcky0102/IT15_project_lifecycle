using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Constants;
using project_lifecycle.Data;
using project_lifecycle.ViewModels;

namespace project_lifecycle.Areas.Executive.Controllers
{
    [Area("Executive")]
    [Authorize(Roles = "Executive")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
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

            var model = new UserListViewModel
            {
                Users = userDetails
            };

            return View(model);
        }
    }
}
