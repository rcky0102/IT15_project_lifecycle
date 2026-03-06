using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;

namespace project_lifecycle.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "HumanResource")]
    public class PositionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _audit;

        public PositionController(ApplicationDbContext context, IAuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string archiveFilter = "active")
        {
            IQueryable<Position> query = _context.Positions;
            if (archiveFilter == "active")
                query = query.Where(p => !p.IsArchived);
            else if (archiveFilter == "inactive")
                query = query.Where(p => p.IsArchived);
            var positions = await query.OrderBy(p => p.Name).ToListAsync();
            ViewData["ArchiveFilter"] = archiveFilter;
            return View("~/Areas/HumanResource/Views/Management/Position/Index.cshtml", positions);
        }

        [HttpGet]
        public async Task<IActionResult> GetPositionById(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null)
                return Json(new { success = false, message = "Position not found." });

            return Json(new {
                success = true,
                id = position.Id,
                name = position.Name,
                description = position.Description,
                dateCreated = position.DateCreated.ToString("MM/dd/yyyy HH:mm:ss")
            });
        }

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

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Position created successfully!" });

                    TempData["Success"] = "Position created successfully!";
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

                return View("~/Areas/HumanResource/Views/Management/Position/Create.cshtml", position);
            }
            catch (Exception ex)
            {
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

                return View("~/Areas/HumanResource/Views/Management/Position/Create.cshtml", position);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,DateCreated")] Position position)
        {
            if (id != position.Id)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Position not found." });
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
                        return Json(new { success = true, message = "Position updated successfully!" });

                    TempData["Success"] = "Position updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PositionExists(position.Id))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return Json(new { success = false, message = "Position not found." });
                        return NotFound();
                    }
                    else throw;
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

            return View("~/Areas/HumanResource/Views/Management/Position/Edit.cshtml", position);
        }

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
                    return Json(new { success = true, message = "Position deleted successfully!" });

                TempData["Success"] = "Position deleted successfully!";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Position not found." });
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("ArchivePosition")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchivePositionConfirmed(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null) { TempData["Error"] = "Position not found."; return RedirectToAction(nameof(Index)); }
            position.IsArchived = true;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "Archive", "Position Management", $"Archived position '{position.Name}' (ID: {position.Id})", "Position", position.Id.ToString());
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "Position archived." });
            TempData["Success"] = "Position archived.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("UnarchivePosition")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchivePositionConfirmed(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null) { TempData["Error"] = "Position not found."; return RedirectToAction(nameof(Index)); }
            position.IsArchived = false;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "Unarchive", "Position Management", $"Restored position '{position.Name}' (ID: {position.Id})", "Position", position.Id.ToString());
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "Position restored." });
            TempData["Success"] = "Position restored.";
            return RedirectToAction(nameof(Index));
        }

        private bool PositionExists(int id)
        {
            return _context.Positions.Any(e => e.Id == id);
        }
    }
}
