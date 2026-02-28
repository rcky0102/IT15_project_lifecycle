using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.ViewModels;

namespace project_lifecycle.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "HumanResource")]
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
            var hr = await _context.HumanResources
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (hr == null) return NotFound();

            var user = await _userManager.FindByIdAsync(userId!);

            var vm = new ProfileViewModel
            {
                Id = hr.Id,
                UserId = hr.UserId,
                Email = user?.Email,
                EmployeeNumber = hr.EmployeeNumber,
                FirstName = hr.FirstName,
                MiddleName = hr.MiddleName,
                LastName = hr.LastName,
                Contact = hr.Contact,
                DepartmentName = hr.Department?.Name,
                PositionTitle = hr.Position?.Name,
                DepartmentId = hr.DepartmentId,
                PositionId = hr.PositionId,
                AddressLine = hr.AddressLine,
                Region = hr.Region,
                Province = hr.Province,
                City = hr.City,
                Barangay = hr.Barangay,
                ProfileImage = hr.ProfileImage,
                RoleName = "Human Resource",
                CreatedDate = hr.CreatedDate
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] ProfileViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var hr = await _context.HumanResources
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (hr == null) return NotFound();

            hr.Contact = model.Contact;
            hr.AddressLine = model.AddressLine;
            hr.Region = model.Region;
            hr.Province = model.Province;
            hr.City = model.City;
            hr.Barangay = model.Barangay;

            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                hr.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
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
