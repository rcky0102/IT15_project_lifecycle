// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using project_lifecycle.Services;

namespace project_lifecycle.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ISecurityLogService _securityLogService;

        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger, UserManager<IdentityUser> userManager, ISecurityLogService securityLogService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _securityLogService = securityLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                    {
                        return RedirectToAction("Index", "SuperAdmin", new { area = "SuperAdmin" });
                    }

                    //if (await _userManager.IsInRoleAsync(user, "Admin"))
                    //{
                    //    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    //}

                    if (await _userManager.IsInRoleAsync(user, "Employee"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Employee" });
                    }

                    if (await _userManager.IsInRoleAsync(user, "HumanResource"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "HumanResource" });
                    }

                    if (await _userManager.IsInRoleAsync(user, "DepartmentHead"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "DepartmentHead" });
                    }

                    if (await _userManager.IsInRoleAsync(user, "Executive"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Executive" });
                    }

                    if (await _userManager.IsInRoleAsync(user, "ProjectManager"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "ProjectManager" });
                    }

                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    // Log failed login attempt and check for suspicious activity
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var userAgent = Request.Headers["User-Agent"].ToString();
                    
                    await _securityLogService.LogFailedLoginAndCheckThresholdAsync(
                        Input.Email, 
                        ipAddress ?? "Unknown", 
                        userAgent ?? "Unknown");

                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
