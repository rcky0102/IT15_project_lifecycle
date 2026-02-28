using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.ViewModels;

namespace project_lifecycle.DepartmentHeadArea.Controllers
{
    [Area("DepartmentHead")]
    [Authorize(Roles = "DepartmentHead")]
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
            var dh = await _context.DepartmentHeads
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (dh == null) return NotFound();

            var user = await _userManager.FindByIdAsync(userId!);

            var vm = new ProfileViewModel
            {
                Id = dh.Id,
                UserId = dh.UserId,
                Email = user?.Email,
                EmployeeNumber = dh.EmployeeNumber,
                FirstName = dh.FirstName,
                MiddleName = dh.MiddleName,
                LastName = dh.LastName,
                Contact = dh.Contact,
                DepartmentName = dh.Department?.Name,
                PositionTitle = dh.Position?.Name,
                DepartmentId = dh.DepartmentId,
                PositionId = dh.PositionId,
                AddressLine = dh.AddressLine,
                Region = dh.Region,
                Province = dh.Province,
                City = dh.City,
                Barangay = dh.Barangay,
                ProfileImage = dh.ProfileImage,
                RoleName = "Department Head",
                CreatedDate = dh.CreatedDate
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] ProfileViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var dh = await _context.DepartmentHeads
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (dh == null) return NotFound();

            dh.Contact = model.Contact;
            dh.AddressLine = model.AddressLine;
            dh.Region = model.Region;
            dh.Province = model.Province;
            dh.City = model.City;
            dh.Barangay = model.Barangay;

            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                dh.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
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
