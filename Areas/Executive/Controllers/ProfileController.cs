using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.ViewModels;

namespace project_lifecycle.ExecutiveArea.Controllers
{
    [Area("Executive")]
    [Authorize(Roles = "Executive")]
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
            var ex = await _context.Executives
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (ex == null) return NotFound();

            var user = await _userManager.FindByIdAsync(userId!);

            var vm = new ProfileViewModel
            {
                Id = ex.Id,
                UserId = ex.UserId,
                Email = user?.Email,
                EmployeeNumber = ex.EmployeeNumber,
                FirstName = ex.FirstName,
                MiddleName = ex.MiddleName,
                LastName = ex.LastName,
                Contact = ex.Contact,
                DepartmentName = ex.Department?.Name,
                PositionTitle = ex.Position?.Name,
                DepartmentId = ex.DepartmentId,
                PositionId = ex.PositionId,
                AddressLine = ex.AddressLine,
                Region = ex.Region,
                Province = ex.Province,
                City = ex.City,
                Barangay = ex.Barangay,
                ProfileImage = ex.ProfileImage,
                RoleName = "Executive",
                CreatedDate = ex.CreatedDate
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] ProfileViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var ex = await _context.Executives
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (ex == null) return NotFound();

            ex.Contact = model.Contact;
            ex.AddressLine = model.AddressLine;
            ex.Region = model.Region;
            ex.Province = model.Province;
            ex.City = model.City;
            ex.Barangay = model.Barangay;

            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                ex.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
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
