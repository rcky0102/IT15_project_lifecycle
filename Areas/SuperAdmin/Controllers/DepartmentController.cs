using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _audit;

        public DepartmentController(ApplicationDbContext context, IAuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: /SuperAdmin/Department/Index
        public async Task<IActionResult> Index(string archiveFilter = "active")
        {
            IQueryable<Department> query = _context.Departments;
            if (archiveFilter == "active")
                query = query.Where(d => !d.IsArchived);
            else if (archiveFilter == "inactive")
                query = query.Where(d => d.IsArchived);
            var departments = await query.OrderBy(d => d.Name).ToListAsync();
            ViewData["ArchiveFilter"] = archiveFilter;
            return View("~/Areas/SuperAdmin/Views/Management/Department/Index.cshtml", departments);
        }

        // GET: /SuperAdmin/Department/Create
        public IActionResult Create()
        {
            return View("~/Areas/SuperAdmin/Views/Management/Department/Create.cshtml");
        }

        // GET: /SuperAdmin/Department/GetDepartmentById/5
        [HttpGet]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }
            
            return Json(new { 
                success = true, 
                id = department.Id,
                name = department.Name,
                description = department.Description,
                dateCreated = department.DateCreated.ToString("MM/dd/yyyy HH:mm:ss")
            });
        }

        // POST: /SuperAdmin/Department/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Department department)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    department.DateCreated = DateTime.Now;
                    _context.Add(department);
                    await _context.SaveChangesAsync();

                    await _audit.LogAsync(User, "Create", "Department Management", $"Created department '{department.Name}'", "Department", department.Id.ToString());

                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Department created successfully!" });
                    }
                    
                    TempData["Success"] = "Department created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                
                // If model state is invalid, return the view with validation errors
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    return Json(new { success = false, errors = errors });
                }
                
                return View("~/Areas/SuperAdmin/Views/Management/Department/Create.cshtml", department);
            }
            catch (Exception ex)
            {
                // Log the error (you can use proper logging here)
                Console.WriteLine($"Error creating department: {ex.Message}");
                ModelState.AddModelError("", "Unable to create department. Please try again.");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    return Json(new { success = false, errors = errors, message = "Unable to create department. Please try again." });
                }
                
                return View("~/Areas/SuperAdmin/Views/Management/Department/Create.cshtml", department);
            }
        }

        // GET: /SuperAdmin/Department/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            
            return View("~/Areas/SuperAdmin/Views/Management/Department/Edit.cshtml", department);
        }

        // POST: /SuperAdmin/Department/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,DateCreated")] Department department)
        {
            if (id != department.Id)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Department not found." });
                }
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();

                    await _audit.LogAsync(User, "Update", "Department Management", $"Updated department '{department.Name}' (ID: {department.Id})", "Department", department.Id.ToString());

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Department updated successfully!" });
                    }
                    
                    TempData["Success"] = "Department updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Json(new { success = false, message = "Department not found." });
                        }
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, errors = errors });
            }
            
            return View("~/Areas/SuperAdmin/Views/Management/Department/Edit.cshtml", department);
        }

        // GET: /SuperAdmin/Department/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            return View("~/Areas/SuperAdmin/Views/Management/Department/Delete.cshtml", department);
        }

        // POST: /SuperAdmin/Department/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                await _audit.LogAsync(User, "Delete", "Department Management", $"Deleted department '{department.Name}' (ID: {department.Id})", "Department", department.Id.ToString());

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Department deleted successfully!" });
                }
                
                TempData["Success"] = "Department deleted successfully!";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Department not found." });
                }
                return NotFound();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }

        [HttpPost, ActionName("ArchiveDepartment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveDepartmentConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) { TempData["Error"] = "Department not found."; return RedirectToAction(nameof(Index)); }
            department.IsArchived = true;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "Archive", "Department Management", $"Archived department '{department.Name}' (ID: {department.Id})", "Department", department.Id.ToString());
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "Department archived." });
            TempData["Success"] = "Department archived.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("UnarchiveDepartment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveDepartmentConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) { TempData["Error"] = "Department not found."; return RedirectToAction(nameof(Index)); }
            department.IsArchived = false;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "Unarchive", "Department Management", $"Restored department '{department.Name}' (ID: {department.Id})", "Department", department.Id.ToString());
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "Department restored." });
            TempData["Success"] = "Department restored.";
            return RedirectToAction(nameof(Index));
        }
    }
}
