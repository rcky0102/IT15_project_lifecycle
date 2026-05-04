using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using project_lifecycle.Constants;
using project_lifecycle.Models;

namespace project_lifecycle.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Seed Roles
            var userManager = service.GetService<UserManager<IdentityUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();
            var context = service.GetService<ApplicationDbContext>();
            var config = service.GetService<IConfiguration>();

            if (!await roleManager.RoleExistsAsync(Roles.HumanResource.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.HumanResource.ToString()));
            
            if (!await roleManager.RoleExistsAsync(Roles.DepartmentHead.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.DepartmentHead.ToString()));
            
            if (!await roleManager.RoleExistsAsync(Roles.Executive.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.Executive.ToString()));
            
            if (!await roleManager.RoleExistsAsync(Roles.SuperAdmin.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.SuperAdmin.ToString()));
            
            if (!await roleManager.RoleExistsAsync(Roles.Employee.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.Employee.ToString()));

            if (!await roleManager.RoleExistsAsync(Roles.ProjectManager.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.ProjectManager.ToString()));

            // Seed Departments
            if (!await context.Departments.AnyAsync())
            {
                var departments = new List<Department>
                {
                    new Department { Name = "Information Technology", Description = "IT and software development" },
                    new Department { Name = "Human Resources", Description = "HR and personnel management" },
                    new Department { Name = "Finance", Description = "Financial planning and accounting" },
                    new Department { Name = "Marketing", Description = "Marketing and sales" },
                    new Department { Name = "Operations", Description = "Business operations and logistics" }
                };
                await context.Departments.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            // Seed Positions
            if (!await context.Positions.AnyAsync())
            {
                var positions = new List<Position>
                {
                    new Position { Name = "Software Developer", Description = "Develops software applications" },
                    new Position { Name = "Project Manager", Description = "Manages projects and teams" },
                    new Position { Name = "HR Manager", Description = "Manages human resources functions" },
                    new Position { Name = "Financial Analyst", Description = "Analyzes financial data" },
                    new Position { Name = "Marketing Specialist", Description = "Handles marketing activities" },
                    new Position { Name = "Operations Manager", Description = "Manages business operations" },
                    new Position { Name = "IT Director", Description = "Leads IT department" },
                    new Position { Name = "CEO", Description = "Chief Executive Officer" },
                    new Position { Name = "CTO", Description = "Chief Technology Officer" },
                    new Position { Name = "CFO", Description = "Chief Financial Officer" }
                };
                await context.Positions.AddRangeAsync(positions);
                await context.SaveChangesAsync();
            }

            // Seed SuperAdmin User
            var saEmail = config["SeedData:SuperAdmin:Email"];
            var saPass = config["SeedData:SuperAdmin:Password"];

            if (string.IsNullOrEmpty(saEmail) || string.IsNullOrEmpty(saPass))
            {
                // Seeding skipped: SuperAdmin credentials must be provided via User Secrets or Environment Variables
                return;
            }

            var superAdmin = new IdentityUser
            {
                UserName = saEmail,
                Email = saEmail,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            var superAdminInDb = await userManager.FindByEmailAsync(superAdmin.Email);
            if (superAdminInDb == null)
            {
                var result = await userManager.CreateAsync(superAdmin, saPass);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin.ToString());
                }
            }

            // Ensure SuperAdmin record exists in the SuperAdmins table
            var seededUser = await userManager.FindByEmailAsync(superAdmin.Email);
            if (seededUser != null)
            {
                var saRecord = await context.SuperAdmins.FirstOrDefaultAsync(s => s.UserId == seededUser.Id);
                if (saRecord == null)
                {
                    await context.SuperAdmins.AddAsync(new SuperAdmin
                    {
                        UserId = seededUser.Id,
                        FirstName = config["SeedData:SuperAdmin:FirstName"] ?? "Super",
                        LastName = config["SeedData:SuperAdmin:LastName"] ?? "Admin",
                        MiddleName = config["SeedData:SuperAdmin:MiddleName"] ?? "",
                        Contact = config["SeedData:SuperAdmin:Contact"] ?? "N/A"
                    });
                    await context.SaveChangesAsync();
                }
            }

            // Fix existing projects that have an empty Status
            var emptyStatusProjects = await context.Projects
                .Where(p => p.Status == null || p.Status == "")
                .ToListAsync();
            if (emptyStatusProjects.Any())
            {
                foreach (var proj in emptyStatusProjects)
                    proj.Status = "Unfinished";
                await context.SaveChangesAsync();
            }

        }
    }
}
