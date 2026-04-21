using CoalShortagePortal.Application.Interfaces;
using CoalShortagePortal.Infrastructure.Services;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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
        private readonly IOtpRateLimitService _otpRateLimitService;

        private const int BLOCK_MINUTES = 5;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<IdentityUser> userManager,
            IDNTCaptchaValidatorService validatorService,
            IEmailService emailService,
            IOtpService otpService,
            IOtpRateLimitService otpRateLimitService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _validatorService = validatorService;
            _emailService = emailService;
            _otpService = otpService;
            _otpRateLimitService = otpRateLimitService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public bool IsOtpBlocked { get; set; }
        public int OtpBlockRemainingMinutes { get; set; }

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

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;

            // Always start with clean Step 1 state
            Input = new InputModel { RequiresOtp = false };
            IsOtpBlocked = false;
            OtpBlockRemainingMinutes = 0;

            // ✅ Check TempData — only set by RedirectToPage from OnPostAsync
            if (TempData.ContainsKey("IsOtpBlocked") && (bool)TempData["IsOtpBlocked"])
            {
                var pendingUserId = TempData["PendingOtpUserId"]?.ToString();

                // ✅ Always verify block against the actual service
                // not just TempData — service is the single source of truth
                if (!string.IsNullOrEmpty(pendingUserId)
                    && _otpRateLimitService.IsBlocked(pendingUserId))
                {
                    // Block is still active — calculate REAL remaining time
                    // from the service, not from TempData
                    var blockedUntil = _otpRateLimitService.GetBlockedUntil(pendingUserId);
                    var realRemainingMinutes = blockedUntil.HasValue
                        ? (int)Math.Ceiling(
                            (blockedUntil.Value - DateTime.UtcNow).TotalMinutes)
                        : BLOCK_MINUTES;

                    IsOtpBlocked = true;
                    OtpBlockRemainingMinutes = realRemainingMinutes;
                    Input.RequiresOtp = true;

                    // ✅ Do NOT re-set TempData here — let it expire naturally
                    // Re-setting causes the infinite loop
                }
                else
                {
                    // Block has expired or userId not found
                    // Clean up rate limit and show fresh login
                    if (!string.IsNullOrEmpty(pendingUserId))
                        _otpRateLimitService.ResetAttempts(pendingUserId);

                    // ✅ Explicitly clear all TempData block keys
                    TempData.Remove("IsOtpBlocked");
                    TempData.Remove("OtpBlockRemainingMinutes");
                    TempData.Remove("PendingOtpUserId");

                    IsOtpBlocked = false;
                    Input.RequiresOtp = false;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Clear ModelState for RequiresOtp so Razor uses model value
            ModelState.Remove("Input.RequiresOtp");

            // Conditionally remove irrelevant validations per step
            if (!Input.RequiresOtp)
                ModelState.Remove("Input.OtpCode");
            else
                ModelState.Remove("Input.Password");

            if (!ModelState.IsValid)
                return Page();

            // ============================================================
            // Step 1: Validate CAPTCHA + Password, then send OTP
            // ============================================================
            if (!Input.RequiresOtp)
            {
                // Validate CAPTCHA
                if (!_validatorService.HasRequestValidCaptchaEntry())
                {
                    ModelState.AddModelError(string.Empty,
                        "Please enter the security code as a number.");
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
                        $"Account is locked. Please try again in " +
                        $"{Math.Ceiling(remainingTime)} minutes.");
                    return Page();
                }

                // Verify password
                var passwordCheck = await _userManager.CheckPasswordAsync(user, Input.Password);
                if (!passwordCheck)
                {
                    await _userManager.AccessFailedAsync(user);

                    var failedCount = await _userManager.GetAccessFailedCountAsync(user);
                    var remainingAttempts = 5 - failedCount;

                    _logger.LogWarning(
                        $"Invalid password for user: {user.UserName}. " +
                        $"Remaining attempts: {remainingAttempts}");

                    if (remainingAttempts > 0)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Invalid login attempt. " +
                            $"You have {remainingAttempts} attempt(s) remaining.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty,
                            "Account has been locked due to multiple failed attempts.");
                    }

                    return Page();
                }

                // Password valid — generate and send OTP
                try
                {
                    var otp = await _otpService.GenerateOtpAsync(user.Id, "Login");
                    await _emailService.SendOtpEmailAsync(user.Email, otp);

                    _logger.LogInformation(
                        $"OTP sent to {user.Email} for user {user.UserName}");

                    TempData["PendingOtpUserId"] = user.Id;

                    Input.RequiresOtp = true;
                    Input.Password = null;

                    TempData["InfoMessage"] =
                        $"An OTP has been sent to your registered email: " +
                        $"{MaskEmail(user.Email)}";

                    return Page();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to send OTP: {ex.Message}");
                    ModelState.AddModelError(string.Empty,
                        "Failed to send OTP. Please try again.");
                    return Page();
                }
            }

            // ============================================================
            // Step 2: Validate OTP and Sign In
            // ============================================================
            if (Input.RequiresOtp)
            {
                var pendingUserId = TempData["PendingOtpUserId"]?.ToString();

                if (string.IsNullOrEmpty(pendingUserId))
                {
                    _logger.LogWarning("PendingOtpUserId not found in TempData.");
                    ModelState.AddModelError(string.Empty,
                        "Your session has expired. Please start over.");
                    Input = new InputModel { RequiresOtp = false };
                    return Page();
                }

                var user = await _userManager.FindByIdAsync(pendingUserId);

                if (user == null)
                {
                    _logger.LogWarning(
                        $"User not found for PendingOtpUserId: {pendingUserId}");
                    ModelState.AddModelError(string.Empty,
                        "Invalid session. Please start over.");
                    Input = new InputModel { RequiresOtp = false };
                    return Page();
                }

                // Check if account locked between Step 1 and Step 2
                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning(
                        $"User locked out during OTP step: {user.UserName}");
                    ModelState.AddModelError(string.Empty,
                        "Account is locked. Please contact support.");
                    return Page();
                }

                // Check if OTP attempts are already blocked
                if (_otpRateLimitService.IsBlocked(pendingUserId))
                {
                    var blockedUntil = _otpRateLimitService.GetBlockedUntil(pendingUserId);
                    var remainingMinutes = blockedUntil.HasValue
                        ? (int)Math.Ceiling(
                            (blockedUntil.Value - DateTime.UtcNow).TotalMinutes)
                        : BLOCK_MINUTES;

                    _logger.LogWarning(
                        $"Blocked OTP attempt for user: {user.UserName}");

                    TempData["IsOtpBlocked"] = true;
                    TempData["OtpBlockRemainingMinutes"] = remainingMinutes; // ✅ int
                    TempData["PendingOtpUserId"] = pendingUserId;

                    return RedirectToPage("./Login");
                }

                // Validate OTP input not empty
                if (string.IsNullOrWhiteSpace(Input.OtpCode))
                {
                    ModelState.AddModelError("Input.OtpCode", "Please enter the OTP.");
                    TempData["PendingOtpUserId"] = pendingUserId;
                    Input.RequiresOtp = true;
                    return Page();
                }

                // Validate OTP
                var otpValid = await _otpService.ValidateOtpAsync(
                    user.Id, Input.OtpCode, "Login");

                if (!otpValid)
                {
                    var nowBlocked = _otpRateLimitService.RegisterFailedAttempt(pendingUserId);
                    var remaining = _otpRateLimitService.GetRemainingAttempts(pendingUserId);

                    _logger.LogWarning(
                        $"Invalid OTP for user: {user.UserName}. " +
                        $"Remaining attempts: {remaining}");

                    if (nowBlocked)
                    {
                        _logger.LogWarning(
                            $"User {user.UserName} OTP blocked after max attempts.");

                        TempData["IsOtpBlocked"] = true;
                        TempData["OtpBlockRemainingMinutes"] = BLOCK_MINUTES; // ✅ int
                        TempData["PendingOtpUserId"] = pendingUserId;

                        return RedirectToPage("./Login");
                    }
                    else
                    {
                        TempData.Keep("PendingOtpUserId");
                        Input.RequiresOtp = true;

                        ModelState.AddModelError("Input.OtpCode",
                            $"Invalid or expired OTP. {remaining} attempt(s) remaining.");

                        return Page();
                    }
                }

                // OTP valid — reset rate limit and sign in
                _otpRateLimitService.ResetAttempts(pendingUserId);

                TempData.Remove("PendingOtpUserId");
                TempData.Remove("IsOtpBlocked");
                TempData.Remove("OtpBlockRemainingMinutes");

                await _signInManager.SignInAsync(user, isPersistent: Input.RememberMe);
                await _userManager.ResetAccessFailedCountAsync(user);

                _logger.LogInformation(
                    $"User {user.UserName} logged in successfully at {DateTime.UtcNow}");

                return LocalRedirect(returnUrl);
            }

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