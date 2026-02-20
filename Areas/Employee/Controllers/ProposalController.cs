using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;
using project_lifecycle.ViewModels;

namespace project_lifecycle.EmployeeArea.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class ProposalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ProposalController> _logger;

        public ProposalController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<ProposalController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Proposals";
            
            try
            {
                // Get current employee ID from user
                var userId = _userManager.GetUserId(User);
                _logger.LogInformation($"Getting proposals for userId: {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("UserId is null or empty");
                    return View(new List<ProjectProposal>());
                }

                var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
                
                if (employee == null)
                {
                    _logger.LogWarning($"No employee found for userId: {userId}");
                    return View(new List<ProjectProposal>());
                }

                _logger.LogInformation($"Found employee: {employee.Id}");

                var proposals = _context.ProjectProposals
                    .Where(p => p.EmployeeId == employee.Id)
                    .OrderByDescending(p => p.DateCreated)
                    .ToList();

                _logger.LogInformation($"Found {proposals.Count} proposals for employee {employee.Id}");

                return View(proposals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProposalController.Index");
                ViewData["ErrorMessage"] = $"Error loading proposals: {ex.Message}";
                return View(new List<ProjectProposal>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "New Proposal";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectProposal proposal)
        {
            try
            {
                _logger.LogInformation("Create action called");
                
                // Get current employee ID from user
                var userId = _userManager.GetUserId(User);
                _logger.LogInformation($"UserId: {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("UserId is null");
                    TempData["ErrorMessage"] = "Could not identify user.";
                    return RedirectToAction("Index");
                }

                var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
                
                if (employee == null)
                {
                    _logger.LogError($"No employee found for userId: {userId}");
                    TempData["ErrorMessage"] = "Could not identify employee. Make sure your employee record exists.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation($"Employee found: {employee.Id}");

                // Validate proposal data
                if (proposal == null)
                {
                    _logger.LogError("Proposal object is null");
                    TempData["ErrorMessage"] = "Proposal data is invalid.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(proposal.Title))
                {
                    _logger.LogWarning("Title is empty");
                    TempData["ErrorMessage"] = "Project title is required.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(proposal.Input))
                {
                    _logger.LogWarning("Input is empty");
                    TempData["ErrorMessage"] = "Input is required.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation($"Proposal validation passed. Title: {proposal.Title}");

                proposal.EmployeeId = employee.Id;
                proposal.DateCreated = DateTime.Now;
                proposal.Status = "Pending";

                _logger.LogInformation($"Adding proposal to context. EmployeeId: {proposal.EmployeeId}, Title: {proposal.Title}");

                _context.ProjectProposals.Add(proposal);
                int saved = _context.SaveChanges();

                _logger.LogInformation($"SaveChanges returned: {saved}");

                if (saved > 0)
                {
                    _logger.LogInformation($"Proposal saved successfully. ProposalId: {proposal.Id}");
                    TempData["SuccessMessage"] = $"Proposal '{proposal.Title}' submitted successfully.";
                }
                else
                {
                    _logger.LogWarning("SaveChanges returned 0 rows affected");
                    TempData["ErrorMessage"] = "Proposal was not saved. Please try again.";
                }

                return RedirectToAction("Index");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update exception occurred");
                TempData["ErrorMessage"] = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Create action");
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadRichText(IFormFile upload)
        {
            try
            {
                if (upload == null || upload.Length == 0)
                {
                    return BadRequest(new { error = new { message = "No file uploaded." } });
                }

                const long maxFileSize = 10 * 1024 * 1024;
                if (upload.Length > maxFileSize)
                {
                    return BadRequest(new { error = new { message = "File too large. Max size is 10 MB." } });
                }

                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg",
                    ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".zip"
                };

                var extension = Path.GetExtension(upload.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = new { message = "Unsupported file type." } });
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "editor");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

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
                _logger.LogError(ex, "Error in UploadRichText");
                return StatusCode(500, new { error = new { message = "Upload failed." } });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Upload(IFormFile upload, string CKEditorFuncNum)
        {
            try
            {
                if (upload == null || upload.Length == 0)
                {
                    var emptyScript = $"<script>window.parent.CKEDITOR.tools.callFunction({CKEditorFuncNum}, '', 'No file uploaded');</script>";
                    return Content(emptyScript, "text/html");
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ckeditor");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(upload.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }

                var url = Url.Content($"~/uploads/ckeditor/{fileName}");
                var successScript = $"<script>window.parent.CKEDITOR.tools.callFunction({CKEditorFuncNum}, '{url}', '');</script>";
                return Content(successScript, "text/html");
            }
            catch (Exception ex)
            {
                var safe = ex.Message.Replace("'", "\\'");
                var errScript = $"<script>window.parent.CKEDITOR.tools.callFunction({CKEditorFuncNum}, '', 'Upload failed: {safe}');</script>";
                _logger.LogError(ex, "Error in CKEditor upload");
                return Content(errScript, "text/html");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Challenge();

                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null) return Forbid();

                var proposal = await _context.ProjectProposals.FirstOrDefaultAsync(p => p.Id == id && p.EmployeeId == employee.Id);
                if (proposal == null) return NotFound();

                var versions = await _context.ProjectProposalVersions
                    .Where(v => v.ProjectProposalId == proposal.Id)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToListAsync();

                ViewBag.ProjectProposalVersions = versions;
                var noteVersions = await _context.ProposalNoteVersions
                    .Where(n => n.ProjectProposalId == proposal.Id)
                    .OrderByDescending(n => n.VersionNumber)
                    .ToListAsync();

                ViewBag.ProposalNoteVersions = noteVersions;

                return View(proposal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proposal details for id {Id}", id);
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Challenge();

                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null) return Forbid();

                var proposal = await _context.ProjectProposals.FirstOrDefaultAsync(p => p.Id == id && p.EmployeeId == employee.Id);
                if (proposal == null) return NotFound();

                var versions = await _context.ProjectProposalVersions
                    .Where(v => v.ProjectProposalId == proposal.Id)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToListAsync();

                var nextVersion = (versions.Any() ? versions.Max(v => v.VersionNumber) : 0) + 1;

                var vm = new ProjectProposalEditViewModel
                {
                    Proposal = proposal,
                    Versions = versions,
                    NextVersionNumber = nextVersion
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proposal edit for id {Id}", id);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectProposalEditViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Challenge();

                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null) return Forbid();

                var proposal = await _context.ProjectProposals.FirstOrDefaultAsync(p => p.Id == id && p.EmployeeId == employee.Id);
                if (proposal == null) return NotFound();

                if (model?.Proposal == null)
                {
                    TempData["ErrorMessage"] = "Invalid submission.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(model.Proposal.Title) || string.IsNullOrWhiteSpace(model.Proposal.Input))
                {
                    TempData["ErrorMessage"] = "Title and Input are required.";
                    return RedirectToAction("Edit", new { id });
                }

                // Save current content as a previous version
                var existingVersions = await _context.ProjectProposalVersions.Where(v => v.ProjectProposalId == proposal.Id).ToListAsync();
                var nextVersion = (existingVersions.Any() ? existingVersions.Max(v => v.VersionNumber) : 0) + 1;

                var previousVersion = new ProjectProposalVersion
                {
                    ProjectProposalId = proposal.Id,
                    VersionNumber = nextVersion,
                    Title = proposal.Title,
                    Input = proposal.Input,
                    EmployeeId = employee.Id,
                    DateCreated = DateTime.Now
                };

                _context.ProjectProposalVersions.Add(previousVersion);

                // Update the main proposal with the new content
                proposal.Title = model.Proposal.Title;
                proposal.Input = model.Proposal.Input;
                proposal.DateCreated = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Proposal updated and previous version saved.";
                return RedirectToAction("Details", new { id = proposal.Id });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DB error updating proposal {Id}", id);
                TempData["ErrorMessage"] = dbEx.InnerException?.Message ?? dbEx.Message;
                return RedirectToAction("Edit", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating proposal {Id}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Edit", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Version(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Challenge();

                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null) return Forbid();

                var version = await _context.ProjectProposalVersions
                    .Include(v => v.ProjectProposal)
                    .FirstOrDefaultAsync(v => v.Id == id && v.EmployeeId == employee.Id);

                if (version == null) return NotFound();

                return View(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proposal version {Id}", id);
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Note(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Challenge();

                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (employee == null) return Forbid();

                var note = await _context.ProposalNoteVersions
                    .Include(n => n.ProjectProposal)
                    .FirstOrDefaultAsync(n => n.Id == id && n.ProjectProposal.EmployeeId == employee.Id);

                if (note == null) return NotFound();

                return View(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proposal note {Id}", id);
                return StatusCode(500);
            }
        }
    }
}
