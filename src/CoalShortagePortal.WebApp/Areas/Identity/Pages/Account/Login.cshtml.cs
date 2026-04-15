using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using CoalShortagePortal.Application.Interfaces;
using DNTCaptcha.Core;

namespace CoalShortagePortal.WebApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IDNTCaptchaValidatorService _validatorService;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<IdentityUser> userManager,
            IDNTCaptchaValidatorService validatorService,
            IEmailService emailService,
            IOtpService otpService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _validatorService = validatorService;
            _emailService = emailService;
            _otpService = otpService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Email / Username")]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            public bool RememberMe { get; set; }

            public bool RequiresOtp { get; set; }

            [Display(Name = "OTP Code")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
            public string OtpCode { get; set; }
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

            ReturnUrl = returnUrl;

            // ✅ FIX: Initialize Input so cshtml doesn't throw NullReferenceException
            Input = new InputModel
            {
                RequiresOtp = false
            };
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // ✅ FIX: Clear ModelState for RequiresOtp so Razor uses model value not cached state
            ModelState.Remove("Input.RequiresOtp");

            // ============================================================
            // Conditionally remove irrelevant validations based on the step
            // ============================================================
            if (!Input.RequiresOtp)
            {
                // Step 1: OtpCode not needed yet
                ModelState.Remove("Input.OtpCode");
            }
            else
            {
                // Step 2: Password not needed again
                ModelState.Remove("Input.Password");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ============================================================
            // Step 1: Validate CAPTCHA + Password, then send OTP
            // ============================================================
            if (!Input.RequiresOtp)
            {
                // Validate CAPTCHA
                if (!_validatorService.HasRequestValidCaptchaEntry())
                {
                    ModelState.AddModelError(string.Empty, "Please enter the security code as a number.");
                    return Page();
                }

                // Find user by email or username
                var user = await _userManager.FindByEmailAsync(Input.Email)
                          ?? await _userManager.FindByNameAsync(Input.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                // Check if account is locked out
                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning($"User account locked out: {user.UserName}");
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    var remainingTime = lockoutEnd.HasValue
                        ? (lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes
                        : 0;

                    ModelState.AddModelError(string.Empty,
                        $"Account is locked. Please try again in {Math.Ceiling(remainingTime)} minutes.");
                    return Page();
                }

                // Verify password
                var passwordCheck = await _userManager.CheckPasswordAsync(user, Input.Password);
                if (!passwordCheck)
                {
                    await _userManager.AccessFailedAsync(user);

                    var failedCount = await _userManager.GetAccessFailedCountAsync(user);
                    var remainingAttempts = 5 - failedCount;

                    _logger.LogWarning($"Invalid password for user: {user.UserName}. Remaining attempts: {remainingAttempts}");

                    if (remainingAttempts > 0)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Invalid login attempt. You have {remainingAttempts} attempt(s) remaining.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty,
                            "Account has been locked due to multiple failed attempts.");
                    }

                    return Page();
                }

                // Password is valid — generate and send OTP
                try
                {
                    var otp = await _otpService.GenerateOtpAsync(user.Id, "Login");
                    await _emailService.SendOtpEmailAsync(user.Email, otp);

                    _logger.LogInformation($"OTP sent to {user.Email} for user {user.UserName}");

                    // ====================================================
                    // SECURITY FIX: Store userId in TempData (server-side)
                    // instead of relying on hidden email field from the form
                    // ====================================================
                    TempData["PendingOtpUserId"] = user.Id;

                    // Move to OTP step
                    Input.RequiresOtp = true;
                    Input.Password = null;

                    TempData["InfoMessage"] = $"An OTP has been sent to your registered email: {MaskEmail(user.Email)}";

                    return Page();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to send OTP: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "Failed to send OTP. Please try again.");
                    return Page();
                }
            }

            // ============================================================
            // Step 2: Validate OTP and Sign In
            // ============================================================
            if (Input.RequiresOtp)
            {
                // SECURITY FIX: Retrieve userId from TempData (server-side)
                // Never trust the hidden email field from the form for this
                var pendingUserId = TempData["PendingOtpUserId"]?.ToString();

                if (string.IsNullOrEmpty(pendingUserId))
                {
                    // TempData expired or was never set — session issue
                    _logger.LogWarning("PendingOtpUserId not found in TempData. Possible session expiry or tampering.");
                    ModelState.AddModelError(string.Empty, "Your session has expired. Please start over.");
                    Input.RequiresOtp = false;
                    return Page();
                }

                // Load user securely from database using server-side userId
                var user = await _userManager.FindByIdAsync(pendingUserId);

                if (user == null)
                {
                    _logger.LogWarning($"User not found for PendingOtpUserId: {pendingUserId}");
                    ModelState.AddModelError(string.Empty, "Invalid session. Please start over.");
                    Input.RequiresOtp = false;
                    return Page();
                }

                // Check if account got locked between Step 1 and Step 2
                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning($"User account locked out during OTP step: {user.UserName}");
                    ModelState.AddModelError(string.Empty, "Account is locked. Please contact support.");
                    return Page();
                }

                // Validate OTP against the server-side userId
                if (string.IsNullOrWhiteSpace(Input.OtpCode))
                {
                    ModelState.AddModelError("Input.OtpCode", "Please enter the OTP.");

                    // Re-set TempData so it persists for the next attempt
                    TempData["PendingOtpUserId"] = pendingUserId;
                    return Page();
                }

                var otpValid = await _otpService.ValidateOtpAsync(user.Id, Input.OtpCode, "Login");

                if (!otpValid)
                {
                    _logger.LogWarning($"Invalid OTP attempt for user: {user.UserName}");
                    ModelState.AddModelError("Input.OtpCode", "Invalid or expired OTP. Please try again.");

                    // Re-set TempData so user can retry OTP without starting over
                    TempData["PendingOtpUserId"] = pendingUserId;
                    Input.RequiresOtp = true;
                    return Page();
                }

                // OTP valid — sign in the user
                await _signInManager.SignInAsync(user, isPersistent: Input.RememberMe);

                // Reset failed access count on successful login
                await _userManager.ResetAccessFailedCountAsync(user);

                _logger.LogInformation($"User {user.UserName} logged in successfully with OTP at {DateTime.UtcNow}");

                return LocalRedirect(returnUrl);
            }

            // Fallback
            return Page();
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "";

            var parts = email.Split('@');
            if (parts.Length != 2)
                return email;

            var username = parts[0];
            var domain = parts[1];

            if (username.Length <= 2)
                return $"{username}***@{domain}";

            var visibleChars = username.Substring(0, 2);
            var maskedPart = new string('*', Math.Min(username.Length - 2, 5));

            return $"{visibleChars}{maskedPart}@{domain}";
        }
    }
}