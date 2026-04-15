using CoalShortagePortal.Core;
using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Data;
using CoalShortagePortal.WebApp.Extensions;
using CoalShortagePortal.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Controllers
{
    [Authorize(Roles = SecurityConstants.AdminRoleString)]
    public class UserManageController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;

        public UserManageController(UserManager<IdentityUser> userManager, ILogger<UserManageController> logger, ApplicationDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            UserListVM vm = new UserListVM();
            vm.Users = new List<UserListItemVM>();

            List<IdentityUser> users = await _userManager.Users.ToListAsync();
            foreach (IdentityUser user in users)
            {
                bool isAdmin = (await _userManager.GetRolesAsync(user)).Any(r => r == SecurityConstants.AdminRoleString);
                if (!isAdmin)
                {
                    vm.Users.Add(new UserListItemVM
                    {
                        UserId = user.Id,
                        Username = user.UserName,
                        Email = user.Email,
                        Phone = user.PhoneNumber,
                        IsLockedOut = await _userManager.IsLockedOutAsync(user),
                        LockoutEnd = await _userManager.GetLockoutEndDateAsync(user)
                    });
                }
            }
            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateVM vm)
        {
            if (ModelState.IsValid)
            {
                // Validate State is required for State_Gen
                if (vm.UserType == "State_Gen" && string.IsNullOrWhiteSpace(vm.State))
                {
                    ModelState.AddModelError("State", "State is required for State_Gen user type.");
                    return View(vm);
                }

                IdentityUser user = new IdentityUser 
                {
                    UserName = vm.Username,
                    Email = vm.Email,
                    LockoutEnabled = true  // ← ADD THIS to enable lockout for new users
                };

                IdentityResult result = await _userManager.CreateAsync(user, vm.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Verify user email
                    string emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    IdentityResult emailVerifiedResult = await _userManager.ConfirmEmailAsync(user, emailToken);
                    if (emailVerifiedResult.Succeeded)
                    {
                        _logger.LogInformation($"Email verified for new user {user.UserName} with id {user.Id} and email {vm.Email}");
                    }
                    else
                    {
                        _logger.LogInformation($"Email verify failed for {user.UserName} with id {user.Id} and email {vm.Email}");
                    }

                    if (!string.IsNullOrWhiteSpace(vm.PhoneNumber))
                    {
                        string phoneVerifyToken = await _userManager.GenerateChangePhoneNumberTokenAsync(user, vm.PhoneNumber);
                        IdentityResult phoneVerifyResult = await _userManager.ChangePhoneNumberAsync(user, vm.PhoneNumber, phoneVerifyToken);
                        _logger.LogInformation($"Phone verified new user {user.UserName} with id {user.Id} and phone {vm.PhoneNumber} = {phoneVerifyResult.Succeeded}");
                    }

                    // Create UserDetails record
                    try
                    {
                        var currentUser = await _userManager.GetUserAsync(User);
                        var currentUserId = currentUser?.Id ?? "system";
                        var currentTime = DateTime.UtcNow;

                        var userDetails = new UserDetails
                        {
                            UserId = user.Id,
                            UserType = vm.UserType,
                            State = vm.UserType == "State_Gen" ? vm.State : null,
                            CreatedById = currentUserId,
                            Created = currentTime,
                            LastModifiedById = currentUserId,
                            LastModified = currentTime
                        };

                        _context.UserDetails.Add(userDetails);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"User details created for {vm.Username} with UserType: {vm.UserType}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to create user details for {vm.Username}: {ex.Message}");
                    }

                    // Create Stage 1 for the generating station
                    try
                    {
                        var currentUser = await _userManager.GetUserAsync(User);
                        var currentUserId = currentUser?.Id ?? "system";
                        var currentTime = DateTime.UtcNow;

                        var genStnStg = new GenStnStg
                        {
                            StationName = vm.Username,
                            Stage = 1,
                            CreatedById = currentUserId,
                            Created = currentTime,
                            LastModifiedById = currentUserId,
                            LastModified = currentTime
                        };

                        _context.GenStnStgs.Add(genStnStg);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Automatically created Stage 1 for generating station: {vm.Username}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to create Stage 1 for {vm.Username}: {ex.Message}");
                    }

                    return RedirectToAction(nameof(Index)).WithSuccess($"New user created with UserType: {vm.UserType} and Stage 1");
                }
                AddErrors(result);
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            IdentityUser user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Get user details
            var userDetails = await _context.UserDetails.FirstOrDefaultAsync(u => u.UserId == id);

            UserEditVM vm = new UserEditVM()
            {
                Email = user.Email,
                Username = user.UserName,
                PhoneNumber = user.PhoneNumber,
                UserType = userDetails?.UserType,
                State = userDetails?.State
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditVM vm)
        {
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    return NotFound();
                }

                // Validate State is required for State_Gen
                if (vm.UserType == "State_Gen" && string.IsNullOrWhiteSpace(vm.State))
                {
                    ModelState.AddModelError("State", "State is required for State_Gen user type.");
                    return View(vm);
                }

                IdentityUser user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                List<IdentityError> identityErrors = new List<IdentityError>();
                string oldUsername = user.UserName;

                // Change password if not null
                string newPassword = vm.Password;
                if (newPassword != null)
                {
                    string passResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    IdentityResult passResetResult = await _userManager.ResetPasswordAsync(user, passResetToken, newPassword);
                    if (passResetResult.Succeeded)
                    {
                        _logger.LogInformation("User password changed");
                    }
                    else
                    {
                        identityErrors.AddRange(passResetResult.Errors);
                    }
                }

                // Change username if changed
                if (user.UserName != vm.Username)
                {
                    IdentityResult usernameChangeResult = await _userManager.SetUserNameAsync(user, vm.Username);
                    if (usernameChangeResult.Succeeded)
                    {
                        _logger.LogInformation("Username changed");

                        // Update StationName in all GenStnStg records
                        try
                        {
                            var stageRecords = await _context.GenStnStgs
                                .Where(g => g.StationName == oldUsername)
                                .ToListAsync();

                            foreach (var stage in stageRecords)
                            {
                                stage.StationName = vm.Username;
                            }

                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Updated {stageRecords.Count} stage records from {oldUsername} to {vm.Username}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to update stage records: {ex.Message}");
                        }
                    }
                    else
                    {
                        identityErrors.AddRange(usernameChangeResult.Errors);
                    }
                }

                // Change email if changed
                if (user.Email != vm.Email)
                {
                    string emailResetToken = await _userManager.GenerateChangeEmailTokenAsync(user, vm.Email);
                    IdentityResult emailChangeResult = await _userManager.ChangeEmailAsync(user, vm.Email, emailResetToken);
                    if (emailChangeResult.Succeeded)
                    {
                        _logger.LogInformation("Email changed");
                    }
                    else
                    {
                        identityErrors.AddRange(emailChangeResult.Errors);
                    }
                }

                // Check if phone number to be changed
                if (user.PhoneNumber != vm.PhoneNumber)
                {
                    string phoneChangeToken = await _userManager.GenerateChangePhoneNumberTokenAsync(user, vm.PhoneNumber);
                    IdentityResult phoneChangeResult = await _userManager.ChangePhoneNumberAsync(user, vm.PhoneNumber, phoneChangeToken);
                    if (phoneChangeResult.Succeeded)
                    {
                        _logger.LogInformation($"Phone number of user {user.UserName} with id {user.Id} changed to {vm.PhoneNumber}");
                    }
                    else
                    {
                        identityErrors.AddRange(phoneChangeResult.Errors);
                    }
                }

                // Update UserDetails
                try
                {
                    var userDetails = await _context.UserDetails.FirstOrDefaultAsync(u => u.UserId == id);
                    var currentUser = await _userManager.GetUserAsync(User);
                    var currentUserId = currentUser?.Id ?? "system";

                    if (userDetails != null)
                    {
                        // Update existing record
                        userDetails.UserType = vm.UserType;
                        userDetails.State = vm.UserType == "State_Gen" ? vm.State : null;
                        userDetails.LastModifiedById = currentUserId;
                        userDetails.LastModified = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new record if doesn't exist
                        userDetails = new UserDetails
                        {
                            UserId = id,
                            UserType = vm.UserType,
                            State = vm.UserType == "State_Gen" ? vm.State : null,
                            CreatedById = currentUserId,
                            Created = DateTime.UtcNow,
                            LastModifiedById = currentUserId,
                            LastModified = DateTime.UtcNow
                        };
                        _context.UserDetails.Add(userDetails);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"User details updated for {user.UserName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to update user details: {ex.Message}");
                }

                // Check if we have any errors and redirect if successful
                if (identityErrors.Count == 0)
                {
                    _logger.LogInformation("User edit operation successful");
                    return RedirectToAction(nameof(Index)).WithSuccess("User editing done");
                }

                AddErrors(identityErrors);
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            IdentityUser user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            UserDeleteVM vm = new UserDeleteVM()
            {
                Email = user.Email,
                Username = user.UserName,
                UserId = user.Id
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(UserDeleteVM vm)
        {
            if (ModelState.IsValid)
            {
                IdentityUser user = await _userManager.FindByIdAsync(vm.UserId);
                if (user == null)
                {
                    return NotFound();
                }

                // Delete UserDetails first (foreign key constraint)
                try
                {
                    var userDetails = await _context.UserDetails.FirstOrDefaultAsync(u => u.UserId == vm.UserId);
                    if (userDetails != null)
                    {
                        _context.UserDetails.Remove(userDetails);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"User details deleted for {user.UserName}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to delete user details: {ex.Message}");
                }

                IdentityResult result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User deleted successfully");
                    return RedirectToAction(nameof(Index)).WithSuccess("User delete done");
                }

                AddErrors(result);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Reset lockout
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                // Reset access failed count
                await _userManager.ResetAccessFailedCountAsync(user);

                _logger.LogInformation($"User {user.UserName} has been unlocked by admin");
                return RedirectToAction(nameof(Index)).WithSuccess($"User <strong>{user.UserName}</strong> has been unlocked successfully");
            }

            return RedirectToAction(nameof(Index)).WithSuccess("Failed to unlock user");
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private void AddErrors(IEnumerable<IdentityError> errs)
        {
            foreach (IdentityError error in errs)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}