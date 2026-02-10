using Microsoft.AspNetCore.Identity;
using project_lifecycle.Constants;

namespace project_lifecycle.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Seed Roles
            var userManager = service.GetService<UserManager<IdentityUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();

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

            // Seed SuperAdmin User
            var superAdmin = new IdentityUser
            {
                UserName = "superadmin@gmail.com",
                Email = "superadmin@gmail.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            var superAdminInDb = await userManager.FindByEmailAsync(superAdmin.Email);
            if (superAdminInDb == null)
            {
                var result = await userManager.CreateAsync(superAdmin, "@Admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin.ToString());
                }
            }

        }
    }
}
