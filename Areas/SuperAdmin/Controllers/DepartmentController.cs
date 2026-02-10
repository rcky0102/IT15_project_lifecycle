using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /SuperAdmin/Department/Index
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .ToListAsync();
            
            return View("~/Areas/SuperAdmin/Views/Management/Department/Index.cshtml", departments);
        }

        // GET: /SuperAdmin/Department/Create
        public IActionResult Create()
        {
            return View("~/Areas/SuperAdmin/Views/Management/Department/Create.cshtml");
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
                    
                    TempData["Success"] = "Department created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                
                // If model state is invalid, return the view with validation errors
                return View("~/Areas/SuperAdmin/Views/Management/Department/Create.cshtml", department);
            }
            catch (Exception ex)
            {
                // Log the error (you can use proper logging here)
                Console.WriteLine($"Error creating department: {ex.Message}");
                ModelState.AddModelError("", "Unable to create department. Please try again.");
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
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Department updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
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
                
                TempData["Success"] = "Department deleted successfully!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
