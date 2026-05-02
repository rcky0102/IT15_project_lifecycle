// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace project_lifecycle.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        // If the user has multiple Google external logins, present them for selection
        public IList<UserLoginInfo> LinkedGoogleLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
            // Selected external login provider key (Google account identifier)
            public string SelectedProviderKey { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // If the request included a selected external provider key, treat as final confirmation
                if (!string.IsNullOrEmpty(Input.SelectedProviderKey))
                {
                    // Ensure the selected provider key belongs to a Google external login on this user
                    var logins = await _userManager.GetLoginsAsync(user);
                    var match = logins?.FirstOrDefault(l => l.LoginProvider == "Google" && l.ProviderKey == Input.SelectedProviderKey);
                    if (match == null)
                    {
                        // No matching Google login — do nothing publicly
                        return RedirectToPage("./ForgotPasswordConfirmation");
                    }

                    // Generate and send password reset to the user's registered email
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Reset Password",
                        $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Otherwise, this is the initial submit: check whether this user has any Google external logins
                var userLogins = await _userManager.GetLoginsAsync(user);
                var googleLogins = new List<UserLoginInfo>();
                if (userLogins != null)
                {
                    foreach (var l in userLogins)
                    {
                        if (l.LoginProvider == "Google")
                        {
                            googleLogins.Add(l);
                        }
                    }
                }

                if (googleLogins.Count == 0)
                {
                    // No Google-linked account — do nothing (preserve generic response)
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // If there's exactly one Google login, send immediately
                if (googleLogins.Count == 1)
                {
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Reset Password",
                        $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Multiple Google logins: show choices so user can pick the account to send to
                LinkedGoogleLogins = googleLogins;
                return Page();
            }

            return Page();
        }
    }
}
