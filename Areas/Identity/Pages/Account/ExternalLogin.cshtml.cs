using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace project_lifecycle.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ExternalLoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
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

            // If this external login was initiated from the Forgot Password flow,
            // send a password reset email to the account linked to this external login (if any).
            var isForgotPasswordFlow = false;
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("forgotPassword=1"))
            {
                isForgotPasswordFlow = true;
            }
            else if (Request?.Query != null && Request.Query.ContainsKey("forgotPassword"))
            {
                isForgotPasswordFlow = string.Equals(Request.Query["forgotPassword"], "1", StringComparison.OrdinalIgnoreCase);
            }

            if (isForgotPasswordFlow)
            {
                // Find a user that has this external login
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    // Prefer sending the reset link to the external Google account email claim
                    var externalEmail = info.Principal?.FindFirstValue(ClaimTypes.Email)
                                        ?? info.Principal?.FindFirst("email")?.Value;

                    if (!string.IsNullOrEmpty(externalEmail))
                    {
                        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ResetPassword",
                            pageHandler: null,
                            values: new { area = "Identity", code },
                            protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(
                            externalEmail,
                            "Reset Password",
                            $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>");
                    }
                    // If there's no external email claim, do nothing (preserve generic response)
                }
                else
                {
                    // No user linked to this external login: notify the forgot-password page
                    TempData["ForgotPasswordError"] = "The selected Google account is not linked to any user account.";

                    // Redirect back to the ForgotPassword page so the user sees the error and can try again
                    return RedirectToPage("./ForgotPassword", new { area = "Identity" });
                }

                return LocalRedirect(Url.Page("./ForgotPasswordConfirmation", new { area = "Identity" }));
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

            // If the account requires two-factor, redirect to the 2FA page.
            // Instead of asking the 2FA page to perform final redirect directly, point
            // the 2FA page back to this ExternalLogin callback so the external-signin
            // can be finalized here and redirect to the role dashboard.
            if (result.RequiresTwoFactor)
            {
                var callbackPage = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = callbackPage, RememberMe = false });
            }

            // If the user does not have an account, notify the user.
            TempData["ExternalLoginError"] = "The selected Google account is not linked to any user account. Please sign in with your email and password first, then link your Google account in your profile.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }
    }
}
