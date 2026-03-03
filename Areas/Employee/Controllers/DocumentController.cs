using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.Services;

namespace project_lifecycle.EmployeeArea.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<DocumentController> _logger;
        private readonly INotificationService _notif;

        public DocumentController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<DocumentController> logger, INotificationService notif)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notif = notif;
        }

        // ─── Helpers ────────────────────────────────────────────────
        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;
            return await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        }

        /// <summary>Returns true if the employee owns the document or is a collaborator.</summary>
        private async Task<bool> HasAccessAsync(int documentId, int employeeId)
        {
            return await _context.Documents.AnyAsync(d => d.Id == documentId && d.OwnerEmployeeId == employeeId)
                || await _context.DocumentCollaborators.AnyAsync(dc => dc.DocumentId == documentId && dc.EmployeeId == employeeId);
        }

        private async Task<bool> CanEditAsync(int documentId, int employeeId)
        {
            if (await _context.Documents.AnyAsync(d => d.Id == documentId && d.OwnerEmployeeId == employeeId))
                return true;
            return await _context.DocumentCollaborators.AnyAsync(dc => dc.DocumentId == documentId && dc.EmployeeId == employeeId && dc.Role == "Editor");
        }

        // ─── Index ──────────────────────────────────────────────────
        public async Task<IActionResult> Index(string filter = "my", string archiveFilter = "active")
        {
            ViewData["Title"] = "Documents";

            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return View(new List<Document>());

            IQueryable<Document> query;

            // archiveFilter: active (default) => show not archived
            //                inactive => show only archived
            //                all => show both
            var onlyArchived = string.Equals(archiveFilter, "inactive", StringComparison.OrdinalIgnoreCase);
            var includeAll = string.Equals(archiveFilter, "all", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(filter, "shared", StringComparison.OrdinalIgnoreCase))
            {
                // Documents shared with me (where I'm a collaborator)
                var sharedDocIds = _context.DocumentCollaborators
                    .Where(dc => dc.EmployeeId == employee.Id)
                    .Select(dc => dc.DocumentId);

                query = _context.Documents
                    .Where(d => sharedDocIds.Contains(d.Id) && (includeAll || (onlyArchived ? d.IsArchived : !d.IsArchived)))
                    .Include(d => d.OwnerEmployee);
            }
            else if (string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase))
            {
                // All documents I own or have access to
                var sharedDocIds = _context.DocumentCollaborators
                    .Where(dc => dc.EmployeeId == employee.Id)
                    .Select(dc => dc.DocumentId);

                query = _context.Documents
                    .Where(d => (d.OwnerEmployeeId == employee.Id || sharedDocIds.Contains(d.Id)) && (includeAll || (onlyArchived ? d.IsArchived : !d.IsArchived)))
                    .Include(d => d.OwnerEmployee);
            }
            else
            {
                // My own documents
                query = _context.Documents
                    .Where(d => d.OwnerEmployeeId == employee.Id && (includeAll || (onlyArchived ? d.IsArchived : !d.IsArchived)))
                    .Include(d => d.OwnerEmployee);
            }

            var documents = await query.OrderByDescending(d => d.LastModified ?? d.DateCreated).ToListAsync();
            ViewData["Filter"] = filter;
            ViewData["ArchiveFilter"] = archiveFilter;
            ViewData["CurrentEmployeeId"] = employee.Id;

            return View(documents);
        }

        // ─── Create (GET) ───────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "New Document";
            return View();
        }

        // ─── Create (POST) ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Title, string Content)
        {
            try
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee profile not found.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(Title))
                {
                    TempData["ErrorMessage"] = "Title is required.";
                    return View();
                }

                var doc = new Document
                {
                    OwnerEmployeeId = employee.Id,
                    Title = Title.Trim(),
                    Content = Content ?? string.Empty,
                    DateCreated = DateTime.Now,
                    LastModified = DateTime.Now
                };

                _context.Documents.Add(doc);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Document created successfully.";
                return RedirectToAction("Edit", new { id = doc.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                TempData["ErrorMessage"] = "An error occurred while creating the document.";
                return View();
            }
        }

        // ─── Edit (GET) ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Index");

            var doc = await _context.Documents
                .Include(d => d.OwnerEmployee)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null)
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Index");
            }

            if (!await HasAccessAsync(id, employee.Id))
            {
                TempData["ErrorMessage"] = "You do not have access to this document.";
                return RedirectToAction("Index");
            }

            var canEdit = await CanEditAsync(id, employee.Id);
            ViewData["Title"] = doc.Title;
            ViewData["CanEdit"] = canEdit;
            ViewData["IsOwner"] = doc.OwnerEmployeeId == employee.Id;
            ViewData["CurrentEmployeeId"] = employee.Id;

            // Load collaborators
            var collaborators = await _context.DocumentCollaborators
                .Where(dc => dc.DocumentId == id)
                .Include(dc => dc.Employee)
                .OrderBy(dc => dc.DateAdded)
                .ToListAsync();
            ViewData["Collaborators"] = collaborators;

            // Load versions
            var versions = await _context.DocumentVersions
                .Where(dv => dv.DocumentId == id)
                .Include(dv => dv.Employee)
                .OrderByDescending(dv => dv.VersionNumber)
                .ToListAsync();
            ViewData["Versions"] = versions;

            return View(doc);
        }

        // ─── Save (AJAX) ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id, string Content, string? Title)
        {
            try
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee == null)
                    return Json(new { success = false, message = "Employee profile not found." });

                var doc = await _context.Documents.FindAsync(id);
                if (doc == null)
                    return Json(new { success = false, message = "Document not found." });

                if (!await CanEditAsync(id, employee.Id))
                    return Json(new { success = false, message = "You do not have edit permission." });

                // ── Create a version snapshot of the current content before overwriting ──
                var lastVersionNumber = await _context.DocumentVersions
                    .Where(dv => dv.DocumentId == id)
                    .OrderByDescending(dv => dv.VersionNumber)
                    .Select(dv => (int?)dv.VersionNumber)
                    .FirstOrDefaultAsync() ?? 0;

                var version = new DocumentVersion
                {
                    DocumentId = id,
                    VersionNumber = lastVersionNumber + 1,
                    Title = doc.Title,
                    Content = doc.Content,
                    EmployeeId = employee.Id,
                    DateCreated = DateTime.Now
                };
                _context.DocumentVersions.Add(version);

                // ── Now update the document ──
                doc.Content = Content ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(Title))
                    doc.Title = Title.Trim();
                doc.LastModified = DateTime.Now;
                await _context.SaveChangesAsync();

                var versionCount = await _context.DocumentVersions.CountAsync(dv => dv.DocumentId == id);

                return Json(new
                {
                    success = true,
                    message = "Saved.",
                    title = doc.Title,
                    lastModified = doc.LastModified?.ToString("MMM dd, yyyy h:mm tt"),
                    versionNumber = version.VersionNumber,
                    versionCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving document {DocumentId}", id);
                return Json(new { success = false, message = "An error occurred while saving." });
            }
        }

        // ─── Version Detail (GET) ───────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Version(int id)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Index");

            var version = await _context.DocumentVersions
                .Include(dv => dv.Document)
                .Include(dv => dv.Employee)
                .FirstOrDefaultAsync(dv => dv.Id == id);

            if (version == null)
            {
                TempData["ErrorMessage"] = "Version not found.";
                return RedirectToAction("Index");
            }

            if (!await HasAccessAsync(version.DocumentId, employee.Id))
            {
                TempData["ErrorMessage"] = "You do not have access to this document.";
                return RedirectToAction("Index");
            }

            ViewData["Title"] = $"Version {version.VersionNumber} - {version.Title}";
            return View(version);
        }

        // ─── Restore Version (POST) ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreVersion(int versionId)
        {
            try
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee profile not found.";
                    return RedirectToAction("Index");
                }

                var version = await _context.DocumentVersions
                    .Include(dv => dv.Document)
                    .FirstOrDefaultAsync(dv => dv.Id == versionId);

                if (version?.Document == null)
                {
                    TempData["ErrorMessage"] = "Version not found.";
                    return RedirectToAction("Index");
                }

                if (!await CanEditAsync(version.DocumentId, employee.Id))
                {
                    TempData["ErrorMessage"] = "You do not have edit permission.";
                    return RedirectToAction("Edit", new { id = version.DocumentId });
                }

                var doc = version.Document;

                // Save current state as a new version before restoring
                var lastVersionNumber = await _context.DocumentVersions
                    .Where(dv => dv.DocumentId == doc.Id)
                    .OrderByDescending(dv => dv.VersionNumber)
                    .Select(dv => (int?)dv.VersionNumber)
                    .FirstOrDefaultAsync() ?? 0;

                _context.DocumentVersions.Add(new DocumentVersion
                {
                    DocumentId = doc.Id,
                    VersionNumber = lastVersionNumber + 1,
                    Title = doc.Title,
                    Content = doc.Content,
                    EmployeeId = employee.Id,
                    DateCreated = DateTime.Now
                });

                // Restore
                doc.Content = version.Content;
                doc.LastModified = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Restored to version {version.VersionNumber}.";
                return RedirectToAction("Edit", new { id = doc.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring version {VersionId}", versionId);
                TempData["ErrorMessage"] = "An error occurred while restoring.";
                return RedirectToAction("Index");
            }
        }

        // ─── Delete (POST) ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee profile not found.";
                    return RedirectToAction("Index");
                }

                var doc = await _context.Documents.FindAsync(id);
                if (doc == null || doc.OwnerEmployeeId != employee.Id)
                {
                    TempData["ErrorMessage"] = "Document not found or you are not the owner.";
                    return RedirectToAction("Index");
                }

                // Remove versions, collaborators, then document
                var versions = _context.DocumentVersions.Where(dv => dv.DocumentId == id);
                _context.DocumentVersions.RemoveRange(versions);
                var collabs = _context.DocumentCollaborators.Where(dc => dc.DocumentId == id);
                _context.DocumentCollaborators.RemoveRange(collabs);
                _context.Documents.Remove(doc);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Document deleted.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting.";
                return RedirectToAction("Index");
            }
        }

        // ─── Archive (POST) ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            try
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee profile not found.";
                    return RedirectToAction("Index");
                }

                var doc = await _context.Documents.FindAsync(id);
                if (doc == null || doc.OwnerEmployeeId != employee.Id)
                {
                    TempData["ErrorMessage"] = "Document not found or you are not the owner.";
                    return RedirectToAction("Index");
                }

                if (doc.IsArchived)
                {
                    TempData["ErrorMessage"] = "Document is already archived.";
                    return RedirectToAction("Index");
                }

                doc.IsArchived = true;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Document archived.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving document {DocumentId}", id);
                TempData["ErrorMessage"] = "An error occurred while archiving.";
                return RedirectToAction("Index");
            }
        }

        // ─── Unarchive (POST) ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee profile not found.";
                    return RedirectToAction("Index");
                }

                var doc = await _context.Documents.FindAsync(id);
                if (doc == null || doc.OwnerEmployeeId != employee.Id)
                {
                    TempData["ErrorMessage"] = "Document not found or you are not the owner.";
                    return RedirectToAction("Index");
                }

                if (!doc.IsArchived)
                {
                    TempData["ErrorMessage"] = "Document is not archived.";
                    return RedirectToAction("Index");
                }

                doc.IsArchived = false;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Document unarchived.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unarchiving document {DocumentId}", id);
                TempData["ErrorMessage"] = "An error occurred while unarchiving.";
                return RedirectToAction("Index");
            }
        }

        // ─── Upload file for editor (AJAX) ──────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadFile(IFormFile upload)
        {
            try
            {
                if (upload == null || upload.Length == 0)
                    return BadRequest(new { error = new { message = "No file uploaded." } });

                const long maxFileSize = 10 * 1024 * 1024;
                if (upload.Length > maxFileSize)
                    return BadRequest(new { error = new { message = "File too large. Max size is 10 MB." } });

                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg",
                    ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".zip"
                };

                var extension = Path.GetExtension(upload.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                    return BadRequest(new { error = new { message = "Unsupported file type." } });

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "editor");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var safeOriginalName = Path.GetFileName(upload.FileName);
                var fileName = $"{Guid.NewGuid()}_{safeOriginalName}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }

                var publicUrl = Url.Content($"~/uploads/editor/{fileName}") ?? $"/uploads/editor/{fileName}";
                return Json(new
                {
                    url = publicUrl,
                    fileName = safeOriginalName,
                    isImage = upload.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DocumentController.UploadFile");
                return StatusCode(500, new { error = new { message = "Upload failed." } });
            }
        }

        // ─── Add Collaborator (POST) ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCollaborator(int documentId, string employeeSearch, string role = "Editor")
        {
            try
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee == null)
                    return Json(new { success = false, message = "Employee profile not found." });

                var doc = await _context.Documents.FindAsync(documentId);
                if (doc == null || doc.OwnerEmployeeId != employee.Id)
                    return Json(new { success = false, message = "Only the document owner can add collaborators." });

                if (string.IsNullOrWhiteSpace(employeeSearch))
                    return Json(new { success = false, message = "Please provide an employee name or number." });

                // Search for the employee by name or employee number
                var search = employeeSearch.Trim().ToLower();
                var targetEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e =>
                        e.Id != employee.Id &&
                        (e.EmployeeNumber.ToLower() == search ||
                         (e.FirstName + " " + e.LastName).ToLower().Contains(search)));

                if (targetEmployee == null)
                    return Json(new { success = false, message = "Employee not found." });

                // Check if already a collaborator
                var exists = await _context.DocumentCollaborators
                    .AnyAsync(dc => dc.DocumentId == documentId && dc.EmployeeId == targetEmployee.Id);
                if (exists)
                    return Json(new { success = false, message = "This employee is already a collaborator." });

                var collab = new DocumentCollaborator
                {
                    DocumentId = documentId,
                    EmployeeId = targetEmployee.Id,
                    Role = role == "Viewer" ? "Viewer" : "Editor",
                    DateAdded = DateTime.Now
                };

                _context.DocumentCollaborators.Add(collab);
                await _context.SaveChangesAsync();

                // Send a notification to the invited employee
                if (targetEmployee.UserId != null)
                {
                    var docLink = Url.Action("Edit", "Document", new { area = "Employee", id = documentId });
                    await _notif.CreateAsync(
                        targetEmployee.UserId,
                        "Document Shared",
                        $"{employee.FirstName} {employee.LastName} shared the document \"{doc.Title}\" with you.",
                        type: "Info",
                        icon: "fas fa-file-alt",
                        link: docLink,
                        module: "Document"
                    );
                }

                return Json(new
                {
                    success = true,
                    message = $"{targetEmployee.FirstName} {targetEmployee.LastName} added as {collab.Role}.",
                    collaborator = new
                    {
                        id = collab.Id,
                        employeeId = targetEmployee.Id,
                        name = $"{targetEmployee.FirstName} {targetEmployee.LastName}",
                        employeeNumber = targetEmployee.EmployeeNumber,
                        role = collab.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding collaborator to document {DocumentId}", documentId);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        // ─── Remove Collaborator (POST) ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCollaborator(int collaboratorId)
        {
            try
            {
                var employee = await GetCurrentEmployeeAsync();
                if (employee == null)
                    return Json(new { success = false, message = "Employee profile not found." });

                var collab = await _context.DocumentCollaborators
                    .Include(dc => dc.Document)
                    .FirstOrDefaultAsync(dc => dc.Id == collaboratorId);

                if (collab == null)
                    return Json(new { success = false, message = "Collaborator not found." });

                // Only document owner can remove collaborators
                if (collab.Document?.OwnerEmployeeId != employee.Id)
                    return Json(new { success = false, message = "Only the document owner can remove collaborators." });

                _context.DocumentCollaborators.Remove(collab);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Collaborator removed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing collaborator {CollaboratorId}", collaboratorId);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        // ─── Search Employees (AJAX) ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> SearchEmployees(string term, int documentId)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
                return Json(new List<object>());

            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            var search = term.Trim().ToLower();

            // Get IDs of employees already on this document
            var existingIds = await _context.DocumentCollaborators
                .Where(dc => dc.DocumentId == documentId)
                .Select(dc => dc.EmployeeId)
                .ToListAsync();

            existingIds.Add(employee.Id); // exclude self

            var results = await _context.Employees
                .Where(e => !existingIds.Contains(e.Id) &&
                    (e.EmployeeNumber.ToLower().Contains(search) ||
                     e.FirstName.ToLower().Contains(search) ||
                     e.LastName.ToLower().Contains(search) ||
                     (e.FirstName + " " + e.LastName).ToLower().Contains(search)))
                .Take(10)
                .Select(e => new
                {
                    id = e.Id,
                    name = e.FirstName + " " + e.LastName,
                    employeeNumber = e.EmployeeNumber
                })
                .ToListAsync();

            return Json(results);
        }

        // ─── Department Employees (AJAX) ──────────────────────────
        [HttpGet]
        public async Task<IActionResult> DepartmentEmployees(int documentId)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
                return Json(new List<object>());

            // Get IDs of employees already on this document
            var existingIds = await _context.DocumentCollaborators
                .Where(dc => dc.DocumentId == documentId)
                .Select(dc => dc.EmployeeId)
                .ToListAsync();

            existingIds.Add(employee.Id); // exclude self

            if (employee.DepartmentId == null)
                return Json(new List<object>());

            var results = await _context.Employees
                .Where(e => e.DepartmentId == employee.DepartmentId && !existingIds.Contains(e.Id))
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new
                {
                    id = e.Id,
                    name = e.FirstName + " " + e.LastName,
                    employeeNumber = e.EmployeeNumber
                })
                .ToListAsync();

            return Json(results);
        }
    }
}
