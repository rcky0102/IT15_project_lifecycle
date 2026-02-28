using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.ViewModels;

namespace project_lifecycle.EmployeeArea.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
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
            var emp = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (emp == null) return NotFound();

            var user = await _userManager.FindByIdAsync(userId!);

            var vm = new ProfileViewModel
            {
                Id = emp.Id,
                UserId = emp.UserId,
                Email = user?.Email,
                EmployeeNumber = emp.EmployeeNumber,
                FirstName = emp.FirstName,
                MiddleName = emp.MiddleName,
                LastName = emp.LastName,
                DepartmentName = emp.Department?.Name,
                PositionTitle = emp.Position?.Name,
                DepartmentId = emp.DepartmentId,
                PositionId = emp.PositionId,
                AddressLine = emp.AddressLine,
                Region = emp.Region,
                Province = emp.Province,
                City = emp.City,
                Barangay = emp.Barangay,
                ProfileImage = emp.ProfileImage,
                RoleName = "Employee",
                DateHired = emp.DateHired
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] ProfileViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var emp = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (emp == null) return NotFound();

            emp.AddressLine = model.AddressLine;
            emp.Region = model.Region;
            emp.Province = model.Province;
            emp.City = model.City;
            emp.Barangay = model.Barangay;

            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                emp.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile);
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
