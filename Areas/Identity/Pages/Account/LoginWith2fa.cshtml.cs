// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace project_lifecycle.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginWith2faModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginWith2faModel> _logger;

        public LoginWith2faModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<LoginWith2faModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Authenticator code")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Remember this machine")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            RememberMe = rememberMe;

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            Input = new InputModel();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToPage("./Login");
            }

            var code = Input.TwoFactorCode?.Replace(" ", string.Empty).Replace("-", string.Empty) ?? string.Empty;

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, rememberMe, Input.RememberMachine);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with 2fa.");

                // Try to get roles from the two-factor user; if that's missing, fall back
                // to the currently signed-in user (the sign-in was just performed).
                IdentityUser? targetUser = user;
                if (targetUser == null)
                {
                    targetUser = await _userManager.GetUserAsync(User);
                }

                var roles = targetUser != null ? await _userManager.GetRolesAsync(targetUser) : new System.Collections.Generic.List<string>();
                var role = roles.FirstOrDefault() ?? string.Empty;

                // If the caller gave us a ReturnUrl that points back to the ExternalLogin
                // callback, forward there so ExternalLogin can finalize the external sign-in
                // (and then perform the role -> dashboard redirect). Otherwise use role mapping.
                if (!string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.Contains("ExternalLogin", System.StringComparison.OrdinalIgnoreCase))
                {
                    return LocalRedirect(ReturnUrl);
                }

                var redirect = role switch
                {
                    "Employee" => Url.Content("~/Employee/Dashboard"),
                    "ProjectManager" => Url.Content("~/ProjectManager/Dashboard"),
                    "DepartmentHead" => Url.Content("~/DepartmentHead/Dashboard"),
                    "HumanResource" => Url.Content("~/HumanResource/Dashboard"),
                    "Executive" => Url.Content("~/Executive/Dashboard"),
                    "SuperAdmin" => Url.Content("~/SuperAdmin/Dashboard"),
                    "Admin" => Url.Content("~/Admin/Dashboard"),
                    _ => returnUrl
                };

                return LocalRedirect(redirect);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }
            if (result.IsNotAllowed)
            {
                _logger.LogWarning("2FA sign-in not allowed for user.");
                ModelState.AddModelError(string.Empty, "Sign-in not allowed for this account. Check account status (email confirmation, disabled, etc.).");
                return Page();
            }

            // Generic failure - surface the SignInResult for debugging so caller can see exact reason.
            _logger.LogWarning("2FA sign-in failed for user. Result: {Result}", result.ToString());
            ModelState.AddModelError(string.Empty, "Invalid authenticator code. (Details: " + result.ToString() + ")");
            return Page();
        }
    }
}
