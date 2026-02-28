using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;

namespace project_lifecycle.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public ProfileApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        // ─── GET api/profileapi/me ───
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";

            object? profile = null;

            switch (role)
            {
                case "Employee":
                    var emp = await _context.Employees.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (emp != null)
                        profile = new
                        {
                            emp.Id, emp.EmployeeNumber, emp.FirstName, emp.MiddleName, emp.LastName,
                            Contact = (string?)null,
                            DepartmentName = emp.Department?.Name,
                            PositionName = emp.Position?.Name,
                            emp.DepartmentId, emp.PositionId,
                            emp.AddressLine, emp.Region, emp.Province, emp.City, emp.Barangay,
                            emp.ProfileImage,
                            DateHired = emp.DateHired.ToString("yyyy-MM-dd")
                        };
                    break;

                case "ProjectManager":
                    var pm = await _context.ProjectManagers.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (pm != null)
                        profile = new
                        {
                            pm.Id, pm.EmployeeNumber, pm.FirstName, pm.MiddleName, pm.LastName,
                            pm.Contact,
                            DepartmentName = pm.Department?.Name,
                            PositionName = pm.Position?.Name,
                            pm.DepartmentId, pm.PositionId,
                            pm.AddressLine, pm.Region, pm.Province, pm.City, pm.Barangay,
                            pm.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;

                case "DepartmentHead":
                    var dh = await _context.DepartmentHeads.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (dh != null)
                        profile = new
                        {
                            dh.Id, dh.EmployeeNumber, dh.FirstName, dh.MiddleName, dh.LastName,
                            dh.Contact,
                            DepartmentName = dh.Department?.Name,
                            PositionName = dh.Position?.Name,
                            dh.DepartmentId, dh.PositionId,
                            dh.AddressLine, dh.Region, dh.Province, dh.City, dh.Barangay,
                            dh.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;

                case "HumanResource":
                    var hr = await _context.HumanResources.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (hr != null)
                        profile = new
                        {
                            hr.Id, hr.EmployeeNumber, hr.FirstName, hr.MiddleName, hr.LastName,
                            hr.Contact,
                            DepartmentName = hr.Department?.Name,
                            PositionName = hr.Position?.Name,
                            hr.DepartmentId, hr.PositionId,
                            hr.AddressLine, hr.Region, hr.Province, hr.City, hr.Barangay,
                            hr.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;

                case "Executive":
                    var ex = await _context.Executives.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (ex != null)
                        profile = new
                        {
                            ex.Id, ex.EmployeeNumber, ex.FirstName, ex.MiddleName, ex.LastName,
                            ex.Contact,
                            DepartmentName = ex.Department?.Name,
                            PositionName = ex.Position?.Name,
                            ex.DepartmentId, ex.PositionId,
                            ex.AddressLine, ex.Region, ex.Province, ex.City, ex.Barangay,
                            ex.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;
            }

            if (profile == null)
            {
                // SuperAdmin or unlinked user – return minimal info
                return Ok(new
                {
                    email = user.Email,
                    role = string.IsNullOrEmpty(role) ? "SuperAdmin" : role,
                    profile = new
                    {
                        Id = 0,
                        EmployeeNumber = "",
                        FirstName = "Super",
                        MiddleName = "",
                        LastName = "Admin",
                        Contact = (string?)null,
                        DepartmentName = (string?)null,
                        PositionName = (string?)null,
                        DepartmentId = (int?)null,
                        PositionId = (int?)null,
                        AddressLine = "",
                        Region = "",
                        Province = "",
                        City = "",
                        Barangay = "",
                        ProfileImage = (string?)null,
                        DateHired = (string?)null
                    }
                });
            }

            return Ok(new { email = user.Email, role, profile });
        }

        // ─── POST api/profileapi/update ───
        [HttpPost("update")]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";

            var form = await Request.ReadFormAsync();

            var firstName = form["FirstName"].ToString().Trim();
            var middleName = form["MiddleName"].ToString().Trim();
            var lastName = form["LastName"].ToString().Trim();
            var contact = form["Contact"].ToString().Trim();
            var addressLine = form["AddressLine"].ToString().Trim();
            var region = form["Region"].ToString().Trim();
            var province = form["Province"].ToString().Trim();
            var city = form["City"].ToString().Trim();
            var barangay = form["Barangay"].ToString().Trim();
            var newEmail = form["Email"].ToString().Trim();
            var currentPassword = form["CurrentPassword"].ToString();
            var newPassword = form["NewPassword"].ToString();
            var profileImageFile = form.Files.GetFile("ProfileImageFile");

            var errors = new List<string>();

            // ── Update email if changed ──
            if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, newEmail);
                if (!setEmailResult.Succeeded)
                {
                    errors.AddRange(setEmailResult.Errors.Select(e => e.Description));
                }
                else
                {
                    await _userManager.SetUserNameAsync(user, newEmail);
                    // Re-confirm the email so the user can still log in
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    await _userManager.ConfirmEmailAsync(user, token);
                    // Refresh the auth cookie so the current session stays valid
                    await _signInManager.RefreshSignInAsync(user);
                }
            }

            // ── Update password if provided ──
            if (!string.IsNullOrEmpty(newPassword))
            {
                if (string.IsNullOrEmpty(currentPassword))
                {
                    errors.Add("Current password is required to change password.");
                }
                else
                {
                    var changeResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                    if (!changeResult.Succeeded)
                        errors.AddRange(changeResult.Errors.Select(e => e.Description));
                }
            }

            // ── Handle profile image ──
            string? imagePath = null;
            if (profileImageFile != null && profileImageFile.Length > 0)
            {
                imagePath = await SaveProfileImageAsync(profileImageFile);
            }

            // ── Update role-specific record ──
            switch (role)
            {
                case "Employee":
                    var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (emp != null)
                    {
                        if (!string.IsNullOrEmpty(firstName)) emp.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) emp.LastName = lastName;
                        emp.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        emp.AddressLine = addressLine;
                        emp.Region = region;
                        emp.Province = province;
                        emp.City = city;
                        emp.Barangay = barangay;
                        if (imagePath != null) emp.ProfileImage = imagePath;
                    }
                    break;

                case "ProjectManager":
                    var pm = await _context.ProjectManagers.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (pm != null)
                    {
                        if (!string.IsNullOrEmpty(firstName)) pm.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) pm.LastName = lastName;
                        pm.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        pm.Contact = string.IsNullOrEmpty(contact) ? pm.Contact : contact;
                        pm.AddressLine = addressLine;
                        pm.Region = region;
                        pm.Province = province;
                        pm.City = city;
                        pm.Barangay = barangay;
                        if (imagePath != null) pm.ProfileImage = imagePath;
                    }
                    break;

                case "DepartmentHead":
                    var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (dh != null)
                    {
                        if (!string.IsNullOrEmpty(firstName)) dh.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) dh.LastName = lastName;
                        dh.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        dh.Contact = string.IsNullOrEmpty(contact) ? dh.Contact : contact;
                        dh.AddressLine = addressLine;
                        dh.Region = region;
                        dh.Province = province;
                        dh.City = city;
                        dh.Barangay = barangay;
                        if (imagePath != null) dh.ProfileImage = imagePath;
                    }
                    break;

                case "HumanResource":
                    var hr = await _context.HumanResources.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (hr != null)
                    {
                        if (!string.IsNullOrEmpty(firstName)) hr.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) hr.LastName = lastName;
                        hr.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        hr.Contact = string.IsNullOrEmpty(contact) ? hr.Contact : contact;
                        hr.AddressLine = addressLine;
                        hr.Region = region;
                        hr.Province = province;
                        hr.City = city;
                        hr.Barangay = barangay;
                        if (imagePath != null) hr.ProfileImage = imagePath;
                    }
                    break;

                case "Executive":
                    var ex = await _context.Executives.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (ex != null)
                    {
                        if (!string.IsNullOrEmpty(firstName)) ex.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) ex.LastName = lastName;
                        ex.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        ex.Contact = string.IsNullOrEmpty(contact) ? ex.Contact : contact;
                        ex.AddressLine = addressLine;
                        ex.Region = region;
                        ex.Province = province;
                        ex.City = city;
                        ex.Barangay = barangay;
                        if (imagePath != null) ex.ProfileImage = imagePath;
                    }
                    break;
            }

            await _context.SaveChangesAsync();

            if (errors.Any())
                return BadRequest(new { success = false, errors });

            return Ok(new { success = true, message = "Profile updated successfully.", imagePath });
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
    }
}
