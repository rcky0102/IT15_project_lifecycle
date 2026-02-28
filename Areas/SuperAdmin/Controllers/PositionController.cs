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
    public class PositionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _audit;

        public PositionController(ApplicationDbContext context, IAuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: /SuperAdmin/Position/Index
        public async Task<IActionResult> Index()
        {
            var positions = await _context.Positions
                .OrderBy(p => p.Name)
                .ToListAsync();
            
            return View("~/Areas/SuperAdmin/Views/Management/Position/Index.cshtml", positions);
        }

        // GET: /SuperAdmin/Position/GetPositionById/5
        [HttpGet]
        public async Task<IActionResult> GetPositionById(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null)
            {
                return Json(new { success = false, message = "Position not found." });
            }
            
            return Json(new { 
                success = true, 
                id = position.Id,
                name = position.Name,
                description = position.Description,
                dateCreated = position.DateCreated.ToString("MM/dd/yyyy HH:mm:ss")
            });
        }

        // POST: /SuperAdmin/Position/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Position position)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    position.DateCreated = DateTime.Now;
                    _context.Add(position);
                    await _context.SaveChangesAsync();

                    await _audit.LogAsync(User, "Create", "Position Management", $"Created position '{position.Name}'", "Position", position.Id.ToString());

                    // Check if request is AJAX
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Position created successfully!" });
                    }
                    
                    TempData["Success"] = "Position created successfully!";
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
                
                return View("~/Areas/SuperAdmin/Views/Management/Position/Create.cshtml", position);
            }
            catch (Exception ex)
            {
                // Log the error (you can use proper logging here)
                Console.WriteLine($"Error creating position: {ex.Message}");
                ModelState.AddModelError("", "Unable to create position. Please try again.");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    return Json(new { success = false, errors = errors, message = "Unable to create position. Please try again." });
                }
                
                return View("~/Areas/SuperAdmin/Views/Management/Position/Create.cshtml", position);
            }
        }

        // POST: /SuperAdmin/Position/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,DateCreated")] Position position)
        {
            if (id != position.Id)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Position not found." });
                }
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(position);
                    await _context.SaveChangesAsync();

                    await _audit.LogAsync(User, "Update", "Position Management", $"Updated position '{position.Name}' (ID: {position.Id})", "Position", position.Id.ToString());

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Position updated successfully!" });
                    }
                    
                    TempData["Success"] = "Position updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PositionExists(position.Id))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Json(new { success = false, message = "Position not found." });
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
            
            return View("~/Areas/SuperAdmin/Views/Management/Position/Edit.cshtml", position);
        }

        // POST: /SuperAdmin/Position/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position != null)
            {
                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();

                await _audit.LogAsync(User, "Delete", "Position Management", $"Deleted position '{position.Name}' (ID: {position.Id})", "Position", position.Id.ToString());

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Position deleted successfully!" });
                }
                
                TempData["Success"] = "Position deleted successfully!";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Position not found." });
                }
                return NotFound();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool PositionExists(int id)
        {
            return _context.Positions.Any(e => e.Id == id);
        }
    }
}
