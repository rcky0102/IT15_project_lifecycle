using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.ViewModels;

namespace project_lifecycle.ProjectManagerArea.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var pm = await _context.ProjectManagers
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (pm == null) return NotFound();

            var user = await _userManager.FindByIdAsync(userId!);

            var vm = new ProfileViewModel
            {
                Id = pm.Id,
                UserId = pm.UserId,
                Email = user?.Email,
                EmployeeNumber = pm.EmployeeNumber,
                FirstName = pm.FirstName,
                MiddleName = pm.MiddleName,
                LastName = pm.LastName,
                Contact = pm.Contact,
                DepartmentName = pm.Department?.Name,
                PositionTitle = pm.Position?.Name,
                DepartmentId = pm.DepartmentId,
                PositionId = pm.PositionId,
                AddressLine = pm.AddressLine,
                Region = pm.Region,
                Province = pm.Province,
                City = pm.City,
                Barangay = pm.Barangay,
                ProfileImage = pm.ProfileImage,
                RoleName = "Project Manager",
                CreatedDate = pm.CreatedDate
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] ProfileViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var pm = await _context.ProjectManagers
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (pm == null) return NotFound();

            pm.Contact = model.Contact;
            pm.AddressLine = model.AddressLine;
            pm.Region = model.Region;
            pm.Province = model.Province;
            pm.City = model.City;
            pm.Barangay = model.Barangay;

            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                pm.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Index");
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
