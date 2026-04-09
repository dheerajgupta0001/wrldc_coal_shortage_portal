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
using SkiaSharp;
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
            // acquire user manager via dependency injection
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            UserListVM vm = new UserListVM();
            vm.Users = new List<UserListItemVM>();
            // get the list of users
            List<IdentityUser> users = await _userManager.Users.ToListAsync();
            foreach (IdentityUser user in users)
            {
                // get user is of admin role
                bool isAdmin = (await _userManager.GetRolesAsync(user)).Any(r => r == SecurityConstants.AdminRoleString);
                if (!isAdmin)
                {
                    // add user to vm only if not admin
                    vm.Users.Add(new UserListItemVM
                    {
                        UserId = user.Id,
                        Username = user.UserName,
                        Email = user.Email,
                        Phone = user.PhoneNumber
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
                IdentityUser user = new IdentityUser { UserName = vm.Username, Email = vm.Email };
                IdentityResult result = await _userManager.CreateAsync(user, vm.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // verify user email
                    string emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    IdentityResult emaiVerifiedResult = await _userManager.ConfirmEmailAsync(user, emailToken);
                    if (emaiVerifiedResult.Succeeded)
                    {
                        _logger.LogInformation($"Email verified for new user {user.UserName} with id {user.Id} and email {vm.Email}");
                    }
                    else
                    {
                        _logger.LogInformation($"Email verify failed for {user.UserName} with id {user.Id} and email {vm.Email} due to errors {emaiVerifiedResult.Errors}");
                    }

                    if (!string.IsNullOrWhiteSpace(vm.PhoneNumber))
                    {
                        // verify phone number
                        string phoneVerifyToken = await _userManager.GenerateChangePhoneNumberTokenAsync(user, vm.PhoneNumber);
                        IdentityResult phoneVeifyResult = await _userManager.ChangePhoneNumberAsync(user, vm.PhoneNumber, phoneVerifyToken);
                        _logger.LogInformation($"Phone verified new user {user.UserName} with id {user.Id} and phone {vm.PhoneNumber} = {phoneVeifyResult.Succeeded}");
                    }

                    // *** NEW: Automatically create Stage 1 for the generating station ***
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
                        // Continue even if stage creation fails - user is already created
                    }

                    return RedirectToAction(nameof(Index)).WithSuccess("New user created with Stage 1");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
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

            UserEditVM vm = new UserEditVM()
            {
                Email = user.Email,
                Username = user.UserName,
                PhoneNumber = user.PhoneNumber
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

                IdentityUser user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                List<IdentityError> identityErrors = new List<IdentityError>();
                // Store old username for updating GenStnStg records
                string oldUsername = user.UserName;
                // change password if not null
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

                // change username if changed
                if (user.UserName != vm.Username)
                {
                    IdentityResult usernameChangeResult = await _userManager.SetUserNameAsync(user, vm.Username);
                    if (usernameChangeResult.Succeeded)
                    {
                        _logger.LogInformation("Username changed");
                        // *** NEW: Update StationName in all GenStnStg records ***
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

                // change email if changed
                if (user.Email != vm.Email)
                {
                    string emailResetToken = await _userManager.GenerateChangeEmailTokenAsync(user, vm.Email);
                    IdentityResult emailChangeResult = await _userManager.ChangeEmailAsync(user, vm.Email, emailResetToken);
                    if (emailChangeResult.Succeeded)
                    {
                        _logger.LogInformation("email changed");
                    }
                    else
                    {
                        identityErrors.AddRange(emailChangeResult.Errors);
                    }
                }

                // check if phone number to be changed
                if (user.PhoneNumber != vm.PhoneNumber)
                {
                    string phoneChangeToken = await _userManager.GenerateChangePhoneNumberTokenAsync(user, vm.PhoneNumber);
                    IdentityResult phoneChangeResult = await _userManager.ChangePhoneNumberAsync(user, vm.PhoneNumber, phoneChangeToken);
                    if (phoneChangeResult.Succeeded)
                    {
                        _logger.LogInformation($"phone number of user {user.UserName} with id {user.Id} changed to {vm.PhoneNumber}");
                    }
                    else
                    {
                        identityErrors.AddRange(phoneChangeResult.Errors);
                    }
                }

                // check if we have any errors and redirect if successful
                if (identityErrors.Count == 0)
                {
                    _logger.LogInformation("User edit operation successful");

                    return RedirectToAction(nameof(Index)).WithSuccess("User editing done"); ;
                }

                AddErrors(identityErrors);
            }

            // If we got this far, something failed, redisplay form
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

                IdentityResult result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User deleted successfully");

                    return RedirectToAction(nameof(Index)).WithSuccess("User delete done"); ;
                }

                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(vm);
        }

        // helper function
        private void AddErrors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // helper function
        private void AddErrors(IEnumerable<IdentityError> errs)
        {
            foreach (IdentityError error in errs)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}