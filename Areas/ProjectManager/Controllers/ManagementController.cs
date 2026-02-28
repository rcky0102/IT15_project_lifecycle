    using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;

namespace project_lifecycle.ProjectManagerArea.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class ManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _audit;

        public ManagementController(ApplicationDbContext context, IAuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        // Milestones
        public async Task<IActionResult> Milestone()
        {
            var milestones = await _context.Milestones.OrderBy(m => m.Name).ToListAsync();
            return View("Milestone/Index", milestones);
        }

        [HttpGet]
        public async Task<IActionResult> GetMilestoneById(int id)
        {
            var milestone = await _context.Milestones.FindAsync(id);
            if (milestone == null)
            {
                return Json(new { success = false, message = "Milestone not found." });
            }

            return Json(new
            {
                success = true,
                id = milestone.Id,
                name = milestone.Name,
                description = milestone.Description,
                dateCreated = milestone.DateCreated.ToString("MM/dd/yyyy HH:mm:ss")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMilestone([Bind("Name,Description")] Milestone milestone)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    milestone.DateCreated = DateTime.Now;
                    _context.Add(milestone);
                    await _context.SaveChangesAsync();

                    await _audit.LogAsync(User, "Create", "Milestone Management", $"Created milestone '{milestone.Name}'", "Milestone", milestone.Id.ToString());

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Milestone created successfully!" });
                    }

                    TempData["Success"] = "Milestone created successfully!";
                    return RedirectToAction(nameof(Milestone));
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                    return Json(new { success = false, errors = errors });
                }

                return View("Milestone/Create", milestone);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating milestone: {ex.Message}");
                ModelState.AddModelError("", "Unable to create milestone. Please try again.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                    return Json(new { success = false, errors = errors, message = "Unable to create milestone. Please try again." });
                }

                return View("Milestone/Create", milestone);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMilestone(int id, [Bind("Id,Name,Description,DateCreated")] Milestone milestone)
        {
            if (id != milestone.Id)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = "Milestone not found." });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(milestone);
                    await _context.SaveChangesAsync();

                    await _audit.LogAsync(User, "Update", "Milestone Management", $"Updated milestone '{milestone.Name}' (ID: {milestone.Id})", "Milestone", milestone.Id.ToString());

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Milestone updated successfully!" });

                    TempData["Success"] = "Milestone updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Milestones.Any(e => e.Id == milestone.Id))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = "Milestone not found." });
                        return NotFound();
                    }
                    else throw;
                }
                return RedirectToAction(nameof(Milestone));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                return Json(new { success = false, errors = errors });
            }

            return View("Milestone/Edit", milestone);
        }

        [HttpPost, ActionName("DeleteMilestone")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMilestoneConfirmed(int id)
        {
            var milestone = await _context.Milestones.FindAsync(id);
            if (milestone != null)
            {
                _context.Milestones.Remove(milestone);
                await _context.SaveChangesAsync();

                await _audit.LogAsync(User, "Delete", "Milestone Management", $"Deleted milestone '{milestone.Name}' (ID: {milestone.Id})", "Milestone", milestone.Id.ToString());

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Milestone deleted successfully!" });

                TempData["Success"] = "Milestone deleted successfully!";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = "Milestone not found." });
                return NotFound();
            }

            return RedirectToAction(nameof(Milestone));
        }

        // Project Roles
        public async Task<IActionResult> ProjectRole()
        {
            var roles = await _context.ProjectRoles.OrderBy(r => r.Name).ToListAsync();
            return View("ProjectRole/Index", roles);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectRoleById(int id)
        {
            var role = await _context.ProjectRoles.FindAsync(id);
            if (role == null) return Json(new { success = false, message = "Project role not found." });

            return Json(new
            {
                success = true,
                id = role.Id,
                name = role.Name,
                description = role.Description,
                dateCreated = role.DateCreated.ToString("MM/dd/yyyy HH:mm:ss")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProjectRole([Bind("Name,Description")] ProjectRole role)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    role.DateCreated = DateTime.Now;
                    _context.Add(role);
                    await _context.SaveChangesAsync();

                    await _audit.LogAsync(User, "Create", "Project Role Management", $"Created project role '{role.Name}'", "ProjectRole", role.Id.ToString());

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Project role created successfully!" });

                    TempData["Success"] = "Project role created successfully!";
                    return RedirectToAction(nameof(ProjectRole));
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                    return Json(new { success = false, errors = errors });
                }

                return View("ProjectRole/Create", role);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating project role: {ex.Message}");
                ModelState.AddModelError("", "Unable to create project role. Please try again.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                    return Json(new { success = false, errors = errors, message = "Unable to create project role. Please try again." });
                }

                return View("ProjectRole/Create", role);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProjectRole(int id, [Bind("Id,Name,Description,DateCreated")] ProjectRole role)
        {
            if (id != role.Id)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = "Project role not found." });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(role);
                    await _context.SaveChangesAsync();

                    await _audit.LogAsync(User, "Update", "Project Role Management", $"Updated project role '{role.Name}' (ID: {role.Id})", "ProjectRole", role.Id.ToString());

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Project role updated successfully!" });

                    TempData["Success"] = "Project role updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ProjectRoles.Any(e => e.Id == role.Id))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = "Project role not found." });
                        return NotFound();
                    }
                    else throw;
                }
                return RedirectToAction(nameof(ProjectRole));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                return Json(new { success = false, errors = errors });
            }

            return View("ProjectRole/Edit", role);
        }

        [HttpPost, ActionName("DeleteProjectRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProjectRoleConfirmed(int id)
        {
            var role = await _context.ProjectRoles.FindAsync(id);
            if (role != null)
            {
                _context.ProjectRoles.Remove(role);
                await _context.SaveChangesAsync();

                await _audit.LogAsync(User, "Delete", "Project Role Management", $"Deleted project role '{role.Name}' (ID: {role.Id})", "ProjectRole", role.Id.ToString());

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Project role deleted successfully!" });

                TempData["Success"] = "Project role deleted successfully!";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = "Project role not found." });
                return NotFound();
            }

            return RedirectToAction(nameof(ProjectRole));
        }
    }
}
