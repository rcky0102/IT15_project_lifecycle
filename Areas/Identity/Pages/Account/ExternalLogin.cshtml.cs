using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace project_lifecycle.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public ExternalLoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public string Provider { get; set; }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                return LocalRedirect(returnUrl);
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return LocalRedirect(returnUrl);
            }

            // Sign in the user with this external login provider if the user already has a login.
            // Do not bypass two-factor here so users with 2FA will be challenged.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);
            if (result.Succeeded)
            {
                // Get the user and determine role
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? string.Empty;

                    // Map role to area/dashboard
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

                return LocalRedirect(returnUrl);
            }

            // If the account requires two-factor, redirect to the 2FA page
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });
            }

            // If the user does not have an account, the ExternalLogin flow in this app
            // may rely on other flows (register/import). For now redirect to returnUrl.
            return LocalRedirect(returnUrl);
        }
    }
}
