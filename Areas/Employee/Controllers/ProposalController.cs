using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Models;

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
        public async Task<IActionResult> Create(ProjectProposal proposal, IFormFile? fileAttachment)
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

                if (string.IsNullOrEmpty(proposal.Description))
                {
                    _logger.LogWarning("Description is empty");
                    TempData["ErrorMessage"] = "Description is required.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation($"Proposal validation passed. Title: {proposal.Title}");

                // Handle file upload
                if (fileAttachment != null && fileAttachment.Length > 0)
                {
                    try
                    {
                        _logger.LogInformation($"Processing file upload: {fileAttachment.FileName}");

                        // Create uploads directory if it doesn't exist
                        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proposals");
                        _logger.LogInformation($"Uploads directory: {uploadsDir}");

                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                            _logger.LogInformation("Created uploads directory");
                        }

                        // Generate unique filename
                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileAttachment.FileName)}";
                        var filePath = Path.Combine(uploadsDir, fileName);
                        _logger.LogInformation($"Saving file to: {filePath}");

                        // Save file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileAttachment.CopyToAsync(stream);
                        }

                        // Store relative path for web access
                        proposal.FileAttachment = $"/uploads/proposals/{fileName}";
                        _logger.LogInformation($"File saved successfully. URL: {proposal.FileAttachment}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error uploading file: {fileAttachment.FileName}");
                        TempData["ErrorMessage"] = $"Error uploading file: {ex.Message}";
                        return RedirectToAction("Index");
                    }
                }

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
    }
}
