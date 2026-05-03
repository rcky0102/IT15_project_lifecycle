using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;
using project_lifecycle.Services;

namespace project_lifecycle.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly IAuditLogService _audit;

        public ProfileApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IWebHostEnvironment env, IAuditLogService audit)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _audit = audit;
        }

        // GET: api/profileapi/logins
        [HttpGet("logins")]
        public async Task<IActionResult> GetExternalLogins()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var logins = await _userManager.GetLoginsAsync(user);
            var result = logins.Select(l => new { loginProvider = l.LoginProvider, providerKey = l.ProviderKey });
            return Ok(result);
        }

        // GET: api/profileapi/external-link
        // Initiates external provider flow to link provider to current user
        [HttpGet("external-link")]
        public IActionResult ExternalLink(string provider)
        {
            if (string.IsNullOrEmpty(provider)) return BadRequest(new { success = false, message = "Provider is required" });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var redirectUrl = Url.Action(nameof(ExternalLinkCallback));
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            // Protect against CSRF for linking
            properties.Items["XsrfId"] = userId;
            // Preserve return URL (profile page) so we can redirect back after callback
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer)) properties.Items["returnUrl"] = referer;

            return Challenge(properties, provider);
        }

        // GET: api/profileapi/external-link-callback
        [HttpGet("external-link-callback")]
        public async Task<IActionResult> ExternalLinkCallback()
        {
            var userId = _userManager.GetUserId(User);
            // When linking, GetExternalLoginInfoAsync expects the XSRF id
            var info = await _signInManager.GetExternalLoginInfoAsync(userId);
            if (info == null)
            {
                return BadRequest(new { success = false, message = "External login information could not be loaded." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Add external login to the user
            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(new { success = false, errors });
            }

            await _audit.LogAsync(User, "Link", "ExternalLogin", $"Linked {info.LoginProvider} to user {user.Email}", "User", user.Id);

            // If the original linking request preserved a returnUrl, use it only if it's safe (local)
            var returnUrl = "/";
            if (info.AuthenticationProperties != null && info.AuthenticationProperties.Items.TryGetValue("returnUrl", out var candidate) && !string.IsNullOrEmpty(candidate))
            {
                // Accept local URLs directly
                if (Url.IsLocalUrl(candidate))
                {
                    returnUrl = candidate;
                }
                else
                {
                    // If an absolute URL was provided and it matches our host, use its path+query
                    if (System.Uri.TryCreate(candidate, System.UriKind.Absolute, out var u))
                    {
                        var host = Request.Host.Host;
                        if (!string.IsNullOrEmpty(host) && string.Equals(u.Host, host, StringComparison.OrdinalIgnoreCase))
                        {
                            returnUrl = u.PathAndQuery;
                        }
                    }
                }
            }

            return Redirect(returnUrl);
        }

        // POST: api/profileapi/unlink-external
        [HttpPost("unlink-external")]
        public async Task<IActionResult> UnlinkExternal([FromQuery] string provider)
        {
            if (string.IsNullOrEmpty(provider)) return BadRequest(new { success = false, message = "Provider is required" });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var logins = await _userManager.GetLoginsAsync(user);
            var login = logins.FirstOrDefault(l => l.LoginProvider.Equals(provider, StringComparison.OrdinalIgnoreCase));
            if (login == null) return BadRequest(new { success = false, message = "External login not found" });

            var result = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
            if (!result.Succeeded)
            {
                return BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
            }

            await _audit.LogAsync(User, "Unlink", "ExternalLogin", $"Unlinked {provider} from user {user.Email}", "User", user.Id);

            return Ok(new { success = true, message = $"{provider} unlinked successfully." });
        }

        // ─── GET api/profileapi/me ───
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";

            object? profile = null;

            switch (role)
            {
                case "Employee":
                    var emp = await _context.Employees.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (emp != null)
                        profile = new
                        {
                            emp.Id, emp.EmployeeNumber, emp.FirstName, emp.MiddleName, emp.LastName,
                            emp.Contact,
                            DepartmentName = emp.Department?.Name,
                            PositionName = emp.Position?.Name,
                            emp.DepartmentId, emp.PositionId,
                            emp.AddressLine, emp.Region, emp.Province, emp.City, emp.Barangay,
                            emp.ProfileImage,
                            DateHired = emp.DateHired.ToString("yyyy-MM-dd")
                        };
                    break;

                case "ProjectManager":
                    var pm = await _context.ProjectManagers.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (pm != null)
                        profile = new
                        {
                            pm.Id, pm.EmployeeNumber, pm.FirstName, pm.MiddleName, pm.LastName,
                            pm.Contact,
                            DepartmentName = pm.Department?.Name,
                            PositionName = pm.Position?.Name,
                            pm.DepartmentId, pm.PositionId,
                            pm.AddressLine, pm.Region, pm.Province, pm.City, pm.Barangay,
                            pm.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;

                case "DepartmentHead":
                    var dh = await _context.DepartmentHeads.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (dh != null)
                        profile = new
                        {
                            dh.Id, dh.EmployeeNumber, dh.FirstName, dh.MiddleName, dh.LastName,
                            dh.Contact,
                            DepartmentName = dh.Department?.Name,
                            PositionName = dh.Position?.Name,
                            dh.DepartmentId, dh.PositionId,
                            dh.AddressLine, dh.Region, dh.Province, dh.City, dh.Barangay,
                            dh.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;

                case "HumanResource":
                    var hr = await _context.HumanResources.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (hr != null)
                        profile = new
                        {
                            hr.Id, hr.EmployeeNumber, hr.FirstName, hr.MiddleName, hr.LastName,
                            hr.Contact,
                            DepartmentName = hr.Department?.Name,
                            PositionName = hr.Position?.Name,
                            hr.DepartmentId, hr.PositionId,
                            hr.AddressLine, hr.Region, hr.Province, hr.City, hr.Barangay,
                            hr.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;

                case "Executive":
                    var ex = await _context.Executives.Include(e => e.Department).Include(e => e.Position)
                        .FirstOrDefaultAsync(e => e.UserId == userId);
                    if (ex != null)
                        profile = new
                        {
                            ex.Id, ex.EmployeeNumber, ex.FirstName, ex.MiddleName, ex.LastName,
                            ex.Contact,
                            DepartmentName = ex.Department?.Name,
                            PositionName = ex.Position?.Name,
                            ex.DepartmentId, ex.PositionId,
                            ex.AddressLine, ex.Region, ex.Province, ex.City, ex.Barangay,
                            ex.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;
                case "SuperAdmin":
                    var sa = await _context.SuperAdmins.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (sa != null)
                        profile = new
                        {
                            sa.Id,
                            EmployeeNumber = "",
                            sa.FirstName,
                            sa.MiddleName,
                            sa.LastName,
                            sa.Contact,
                            DepartmentName = (string?)null,
                            PositionName = (string?)null,
                            DepartmentId = (int?)null,
                            PositionId = (int?)null,
                            AddressLine = "",
                            Region = "",
                            Province = "",
                            City = "",
                            Barangay = "",
                            sa.ProfileImage,
                            DateHired = (string?)null
                        };
                    break;
            }

            var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

            if (profile == null)
            {
                // Fallback for SuperAdmin if no record exists or unlinked user – return minimal info
                return Ok(new
                {
                    email = user.Email,
                    role = string.IsNullOrEmpty(role) ? "SuperAdmin" : role,
                    twoFactorEnabled,
                    profile = new
                    {
                        Id = 0,
                        EmployeeNumber = "",
                        FirstName = "Super",
                        MiddleName = "",
                        LastName = "Admin",
                        Contact = (string?)null,
                        DepartmentName = (string?)null,
                        PositionName = (string?)null,
                        DepartmentId = (int?)null,
                        PositionId = (int?)null,
                        AddressLine = "",
                        Region = "",
                        Province = "",
                        City = "",
                        Barangay = "",
                        ProfileImage = (string?)null,
                        DateHired = (string?)null
                    }
                });
            }

            return Ok(new { email = user.Email, role, twoFactorEnabled, profile });
        }

        // GET: api/profileapi/check-email?email=...
        [HttpGet("check-email")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { available = false, message = "Email is required" });
            var found = await _userManager.FindByEmailAsync(email.Trim());
            var available = found == null;
            return Ok(new { available, message = available ? "Available" : "Taken" });
        }

        public class MfaDto
        {
            public bool Enabled { get; set; }
        }

        // POST: api/profileapi/mfa
        [HttpPost("mfa")]
        public async Task<IActionResult> SetMfa([FromBody] MfaDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _userManager.SetTwoFactorEnabledAsync(user, dto?.Enabled == true);
            if (!result.Succeeded)
            {
                return BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
            }

            await _audit.LogAsync(User, "Update", "Security", $"Set MFA {(dto.Enabled ? "enabled" : "disabled")}", "User", user.Id);

            return Ok(new { success = true, twoFactorEnabled = dto.Enabled });
        }

        // GET: api/profileapi/mfa/enroll
        [HttpGet("mfa/enroll")]
        public async Task<IActionResult> EnrollMfa()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Ensure authenticator key exists
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var issuer = "ProjectLifecycle";
            var email = user.Email ?? user.UserName ?? "user";
            var otpauth = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

            // Generate QR image as data URL using QRCoder
            try
            {
                using (var qrGenerator = new QRCoder.QRCodeGenerator())
                using (var qrData = qrGenerator.CreateQrCode(otpauth, QRCoder.QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCoder.PngByteQRCode(qrData))
                {
                    var pngBytes = qrCode.GetGraphic(20);
                    var base64 = Convert.ToBase64String(pngBytes);
                    var dataUrl = $"data:image/png;base64,{base64}";
                    return Ok(new { sharedKey = key, otpauthUri = otpauth, qrDataUrl = dataUrl });
                }
            }
            catch (Exception ex)
            {
                // Fallback to Google Charts URL if QR generation fails
                var qrUrl = "https://chart.googleapis.com/chart?chs=200x200&chld=M|0&cht=qr&chl=" + System.Net.WebUtility.UrlEncode(otpauth);
                return Ok(new { sharedKey = key, otpauthUri = otpauth, qrUrl });
            }
        }

        public class VerifyMfaDto
        {
            public string? Code { get; set; }
        }

        // POST: api/profileapi/mfa/verify
        [HttpPost("mfa/verify")]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (dto == null || string.IsNullOrEmpty(dto.Code)) return BadRequest(new { success = false, message = "Code is required" });

            // Verify the code using the authenticator provider
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, dto.Code.Replace(" ", ""));
            if (!isValid)
            {
                return BadRequest(new { success = false, message = "Invalid verification code." });
            }

            // Enable two-factor and generate recovery codes
            var setResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!setResult.Succeeded)
            {
                return BadRequest(new { success = false, errors = setResult.Errors.Select(e => e.Description) });
            }

            var recovery = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            await _audit.LogAsync(User, "Update", "Security", "Enabled authenticator app MFA", "User", user.Id);

            return Ok(new { success = true, recoveryCodes = recovery });
        }

        // ─── POST api/profileapi/update ───
        [HttpPost("update")]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";

            var form = await Request.ReadFormAsync();

            var firstName = form["FirstName"].ToString().Trim();
            var middleName = form["MiddleName"].ToString().Trim();
            var lastName = form["LastName"].ToString().Trim();
            var contact = form["Contact"].ToString().Trim();
            var addressLine = form["AddressLine"].ToString().Trim();
            var region = form["Region"].ToString().Trim();
            var province = form["Province"].ToString().Trim();
            var city = form["City"].ToString().Trim();
            var barangay = form["Barangay"].ToString().Trim();
            var newEmail = form["Email"].ToString().Trim();
            var currentPassword = form["CurrentPassword"].ToString();
            var newPassword = form["NewPassword"].ToString();
            var profileImageFile = form.Files.GetFile("ProfileImageFile");

            var errors = new List<string>();

            // ── Update email if changed ──
            if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email)
            {
                // Ensure the email isn't used by another account
                var existing = await _userManager.FindByEmailAsync(newEmail);
                if (existing != null && existing.Id != user.Id)
                {
                    errors.Add("The provided email address is already in use.");
                }
                else
                {
                var setEmailResult = await _userManager.SetEmailAsync(user, newEmail);
                if (!setEmailResult.Succeeded)
                {
                    errors.AddRange(setEmailResult.Errors.Select(e => e.Description));
                }
                else
                {
                    await _userManager.SetUserNameAsync(user, newEmail);
                    // Re-confirm the email so the user can still log in
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    await _userManager.ConfirmEmailAsync(user, token);
                    // Refresh the auth cookie so the current session stays valid
                    await _signInManager.RefreshSignInAsync(user);
                }
                }
            }

            // ── Update password if provided ──
            if (!string.IsNullOrEmpty(newPassword))
            {
                if (string.IsNullOrEmpty(currentPassword))
                {
                    errors.Add("Current password is required to change password.");
                }
                else
                {
                    var changeResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                    if (!changeResult.Succeeded)
                        errors.AddRange(changeResult.Errors.Select(e => e.Description));
                }
            }

            // ── Handle profile image ──
            string? imagePath = null;
            if (profileImageFile != null && profileImageFile.Length > 0)
            {
                imagePath = await SaveProfileImageAsync(profileImageFile);
            }

            // ── Update role-specific record ──
            switch (role)
            {
                case "Employee":
                    var emp = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (emp != null)
                    {
                        var empChanges = new List<string>();
                        if (!string.IsNullOrEmpty(firstName) && firstName != emp.FirstName) empChanges.Add("First Name");
                        if (!string.IsNullOrEmpty(lastName) && lastName != emp.LastName) empChanges.Add("Last Name");
                        if (middleName != (emp.MiddleName ?? "")) empChanges.Add("Middle Name");
                        if (!string.IsNullOrEmpty(contact) && contact != (emp.Contact ?? "")) empChanges.Add("Contact");
                        if (addressLine != (emp.AddressLine ?? "")) empChanges.Add("Address Line");
                        if (region != (emp.Region ?? "")) empChanges.Add("Region");
                        if (province != (emp.Province ?? "")) empChanges.Add("Province");
                        if (city != (emp.City ?? "")) empChanges.Add("City");
                        if (barangay != (emp.Barangay ?? "")) empChanges.Add("Barangay");
                        if (imagePath != null) empChanges.Add("Profile Image");
                        if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email) empChanges.Add("Email");
                        if (!string.IsNullOrEmpty(newPassword)) empChanges.Add("Password");

                        if (!string.IsNullOrEmpty(firstName)) emp.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) emp.LastName = lastName;
                        emp.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        emp.Contact = string.IsNullOrEmpty(contact) ? emp.Contact : contact;
                        emp.AddressLine = addressLine;
                        emp.Region = region;
                        emp.Province = province;
                        emp.City = city;
                        emp.Barangay = barangay;
                        if (imagePath != null) emp.ProfileImage = imagePath;

                        if (empChanges.Any())
                        {
                            var desc = $"{emp.FirstName} {emp.LastName} updated profile: {string.Join(", ", empChanges)}";
                            await _audit.LogAsync(User, "Update", "Profile", desc, "Employee", emp.Id.ToString());
                        }
                    }
                    break;

                case "ProjectManager":
                    var pm = await _context.ProjectManagers.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (pm != null)
                    {
                        var pmChanges = new List<string>();
                        if (!string.IsNullOrEmpty(firstName) && firstName != pm.FirstName) pmChanges.Add("First Name");
                        if (!string.IsNullOrEmpty(lastName) && lastName != pm.LastName) pmChanges.Add("Last Name");
                        if (middleName != (pm.MiddleName ?? "")) pmChanges.Add("Middle Name");
                        if (!string.IsNullOrEmpty(contact) && contact != pm.Contact) pmChanges.Add("Contact");
                        if (addressLine != (pm.AddressLine ?? "")) pmChanges.Add("Address Line");
                        if (region != (pm.Region ?? "")) pmChanges.Add("Region");
                        if (province != (pm.Province ?? "")) pmChanges.Add("Province");
                        if (city != (pm.City ?? "")) pmChanges.Add("City");
                        if (barangay != (pm.Barangay ?? "")) pmChanges.Add("Barangay");
                        if (imagePath != null) pmChanges.Add("Profile Image");
                        if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email) pmChanges.Add("Email");
                        if (!string.IsNullOrEmpty(newPassword)) pmChanges.Add("Password");

                        if (!string.IsNullOrEmpty(firstName)) pm.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) pm.LastName = lastName;
                        pm.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        pm.Contact = string.IsNullOrEmpty(contact) ? pm.Contact : contact;
                        pm.AddressLine = addressLine;
                        pm.Region = region;
                        pm.Province = province;
                        pm.City = city;
                        pm.Barangay = barangay;
                        if (imagePath != null) pm.ProfileImage = imagePath;

                        if (pmChanges.Any())
                        {
                            var desc = $"{pm.FirstName} {pm.LastName} updated profile: {string.Join(", ", pmChanges)}";
                            await _audit.LogAsync(User, "Update", "Profile", desc, "ProjectManager", pm.Id.ToString());
                        }
                    }
                    break;

                case "DepartmentHead":
                    var dh = await _context.DepartmentHeads.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (dh != null)
                    {
                        var dhChanges = new List<string>();
                        if (!string.IsNullOrEmpty(firstName) && firstName != dh.FirstName) dhChanges.Add("First Name");
                        if (!string.IsNullOrEmpty(lastName) && lastName != dh.LastName) dhChanges.Add("Last Name");
                        if (middleName != (dh.MiddleName ?? "")) dhChanges.Add("Middle Name");
                        if (!string.IsNullOrEmpty(contact) && contact != dh.Contact) dhChanges.Add("Contact");
                        if (addressLine != (dh.AddressLine ?? "")) dhChanges.Add("Address Line");
                        if (region != (dh.Region ?? "")) dhChanges.Add("Region");
                        if (province != (dh.Province ?? "")) dhChanges.Add("Province");
                        if (city != (dh.City ?? "")) dhChanges.Add("City");
                        if (barangay != (dh.Barangay ?? "")) dhChanges.Add("Barangay");
                        if (imagePath != null) dhChanges.Add("Profile Image");
                        if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email) dhChanges.Add("Email");
                        if (!string.IsNullOrEmpty(newPassword)) dhChanges.Add("Password");

                        if (!string.IsNullOrEmpty(firstName)) dh.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) dh.LastName = lastName;
                        dh.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        dh.Contact = string.IsNullOrEmpty(contact) ? dh.Contact : contact;
                        dh.AddressLine = addressLine;
                        dh.Region = region;
                        dh.Province = province;
                        dh.City = city;
                        dh.Barangay = barangay;
                        if (imagePath != null) dh.ProfileImage = imagePath;

                        if (dhChanges.Any())
                        {
                            var desc = $"{dh.FirstName} {dh.LastName} updated profile: {string.Join(", ", dhChanges)}";
                            await _audit.LogAsync(User, "Update", "Profile", desc, "DepartmentHead", dh.Id.ToString());
                        }
                    }
                    break;

                case "HumanResource":
                    var hr = await _context.HumanResources.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (hr != null)
                    {
                        var changedFields = new List<string>();
                        if (!string.IsNullOrEmpty(firstName) && firstName != hr.FirstName) changedFields.Add("First Name");
                        if (!string.IsNullOrEmpty(lastName) && lastName != hr.LastName) changedFields.Add("Last Name");
                        if (middleName != (hr.MiddleName ?? "")) changedFields.Add("Middle Name");
                        if (!string.IsNullOrEmpty(contact) && contact != hr.Contact) changedFields.Add("Contact");
                        if (addressLine != (hr.AddressLine ?? "")) changedFields.Add("Address Line");
                        if (region != (hr.Region ?? "")) changedFields.Add("Region");
                        if (province != (hr.Province ?? "")) changedFields.Add("Province");
                        if (city != (hr.City ?? "")) changedFields.Add("City");
                        if (barangay != (hr.Barangay ?? "")) changedFields.Add("Barangay");
                        if (imagePath != null) changedFields.Add("Profile Image");
                        if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email) changedFields.Add("Email");
                        if (!string.IsNullOrEmpty(newPassword)) changedFields.Add("Password");

                        if (!string.IsNullOrEmpty(firstName)) hr.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) hr.LastName = lastName;
                        hr.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        hr.Contact = string.IsNullOrEmpty(contact) ? hr.Contact : contact;
                        hr.AddressLine = addressLine;
                        hr.Region = region;
                        hr.Province = province;
                        hr.City = city;
                        hr.Barangay = barangay;
                        if (imagePath != null) hr.ProfileImage = imagePath;

                        if (changedFields.Any())
                        {
                            var desc = $"{hr.FirstName} {hr.LastName} updated profile: {string.Join(", ", changedFields)}";
                            await _audit.LogAsync(User, "Update", "Profile", desc, "HumanResource", hr.Id.ToString());
                        }
                    }
                    break;

                case "Executive":
                    var ex = await _context.Executives.FirstOrDefaultAsync(e => e.UserId == userId);
                    if (ex != null)
                    {
                        var exChanges = new List<string>();
                        if (!string.IsNullOrEmpty(firstName) && firstName != ex.FirstName) exChanges.Add("First Name");
                        if (!string.IsNullOrEmpty(lastName) && lastName != ex.LastName) exChanges.Add("Last Name");
                        if (middleName != (ex.MiddleName ?? "")) exChanges.Add("Middle Name");
                        if (!string.IsNullOrEmpty(contact) && contact != ex.Contact) exChanges.Add("Contact");
                        if (addressLine != (ex.AddressLine ?? "")) exChanges.Add("Address Line");
                        if (region != (ex.Region ?? "")) exChanges.Add("Region");
                        if (province != (ex.Province ?? "")) exChanges.Add("Province");
                        if (city != (ex.City ?? "")) exChanges.Add("City");
                        if (barangay != (ex.Barangay ?? "")) exChanges.Add("Barangay");
                        if (imagePath != null) exChanges.Add("Profile Image");
                        if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email) exChanges.Add("Email");
                        if (!string.IsNullOrEmpty(newPassword)) exChanges.Add("Password");

                        if (!string.IsNullOrEmpty(firstName)) ex.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) ex.LastName = lastName;
                        ex.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        ex.Contact = string.IsNullOrEmpty(contact) ? ex.Contact : contact;
                        ex.AddressLine = addressLine;
                        ex.Region = region;
                        ex.Province = province;
                        ex.City = city;
                        ex.Barangay = barangay;
                        if (imagePath != null) ex.ProfileImage = imagePath;

                        if (exChanges.Any())
                        {
                            var desc = $"{ex.FirstName} {ex.LastName} updated profile: {string.Join(", ", exChanges)}";
                            await _audit.LogAsync(User, "Update", "Profile", desc, "Executive", ex.Id.ToString());
                        }
                    }
                    break;
                case "SuperAdmin":
                    var sa = await _context.SuperAdmins.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (sa != null)
                    {
                        var saChanges = new List<string>();
                        if (!string.IsNullOrEmpty(firstName) && firstName != sa.FirstName) saChanges.Add("First Name");
                        if (!string.IsNullOrEmpty(lastName) && lastName != sa.LastName) saChanges.Add("Last Name");
                        if (middleName != (sa.MiddleName ?? "")) saChanges.Add("Middle Name");
                        if (!string.IsNullOrEmpty(contact) && contact != sa.Contact) saChanges.Add("Contact");
                        if (imagePath != null) saChanges.Add("Profile Image");
                        if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email) saChanges.Add("Email");
                        if (!string.IsNullOrEmpty(newPassword)) saChanges.Add("Password");

                        if (!string.IsNullOrEmpty(firstName)) sa.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName)) sa.LastName = lastName;
                        sa.MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName;
                        sa.Contact = string.IsNullOrEmpty(contact) ? sa.Contact : contact;
                        if (imagePath != null) sa.ProfileImage = imagePath;

                        if (saChanges.Any())
                        {
                            var desc = $"{sa.FirstName} {sa.LastName} updated profile: {string.Join(", ", saChanges)}";
                            await _audit.LogAsync(User, "Update", "Profile", desc, "SuperAdmin", sa.Id.ToString());
                        }
                    }
                    break;
            }

            await _context.SaveChangesAsync();

            if (errors.Any())
                return BadRequest(new { success = false, errors });

            return Ok(new { success = true, message = "Profile updated successfully.", imagePath });
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
