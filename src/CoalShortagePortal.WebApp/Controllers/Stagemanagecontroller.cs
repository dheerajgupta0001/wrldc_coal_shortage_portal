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
    public class StageManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger _logger;

        public StageManageController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<StageManageController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new StageListVM
            {
                Stages = await _context.GenStnStgs
                    .OrderBy(g => g.StationName)
                    .ThenBy(g => g.Stage)
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new StageCreateVM
            {
                AvailableStations = await GetAvailableStationsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StageCreateVM vm)
        {
            if (ModelState.IsValid)
            {
                // Check if the stage already exists
                var existingStage = await _context.GenStnStgs
                    .FirstOrDefaultAsync(g => g.StationName == vm.StationName && g.Stage == vm.Stage);

                if (existingStage != null)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Stage {vm.Stage} already exists for {vm.StationName}");
                    vm.AvailableStations = await GetAvailableStationsAsync();
                    return View(vm);
                }

                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var currentUserId = currentUser?.Id ?? "system";
                    var currentTime = DateTime.UtcNow;

                    var genStnStg = new GenStnStg
                    {
                        StationName = vm.StationName,
                        Stage = vm.Stage,
                        CreatedById = currentUserId,
                        Created = currentTime,
                        LastModifiedById = currentUserId,
                        LastModified = currentTime
                    };

                    _context.GenStnStgs.Add(genStnStg);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Stage {vm.Stage} created for {vm.StationName} by user {currentUserId}");

                    return RedirectToAction(nameof(Index))
                        .WithSuccess($"Stage {vm.Stage} created successfully for {vm.StationName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error creating stage: {ex.Message}");
                    _logger.LogError($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError(string.Empty, $"Error creating stage: {ex.Message}");
                }
            }

            vm.AvailableStations = await GetAvailableStationsAsync();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var stage = await _context.GenStnStgs.FindAsync(id);
            if (stage == null)
            {
                return NotFound();
            }

            var vm = new StageEditVM
            {
                Id = stage.Id,
                StationName = stage.StationName,
                Stage = stage.Stage,
                AvailableStations = await GetAvailableStationsAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StageEditVM vm)
        {
            if (id != vm.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var stage = await _context.GenStnStgs.FindAsync(id);
                if (stage == null)
                {
                    return NotFound();
                }

                // Check if the new combination already exists (excluding current record)
                var existingStage = await _context.GenStnStgs
                    .FirstOrDefaultAsync(g => g.Id != id &&
                                            g.StationName == vm.StationName &&
                                            g.Stage == vm.Stage);

                if (existingStage != null)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Stage {vm.Stage} already exists for {vm.StationName}");
                    vm.AvailableStations = await GetAvailableStationsAsync();
                    return View(vm);
                }

                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var currentUserId = currentUser?.Id ?? "system";
                    var currentTime = DateTime.UtcNow;

                    stage.StationName = vm.StationName;
                    stage.Stage = vm.Stage;
                    stage.LastModifiedById = currentUserId;
                    stage.LastModified = currentTime;

                    _context.Update(stage);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Stage {stage.Id} updated by user {currentUserId}");

                    return RedirectToAction(nameof(Index))
                        .WithSuccess("Stage updated successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating stage: {ex.Message}");
                    ModelState.AddModelError(string.Empty, $"Error updating stage: {ex.Message}");
                }
            }

            vm.AvailableStations = await GetAvailableStationsAsync();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var stage = await _context.GenStnStgs.FindAsync(id);
            if (stage == null)
            {
                return NotFound();
            }

            var vm = new StageDeleteVM
            {
                Id = stage.Id,
                StationName = stage.StationName,
                Stage = stage.Stage
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(StageDeleteVM vm)
        {
            if (ModelState.IsValid)
            {
                var stage = await _context.GenStnStgs.FindAsync(vm.Id);
                if (stage == null)
                {
                    return NotFound();
                }

                try
                {
                    _context.GenStnStgs.Remove(stage);
                    await _context.SaveChangesAsync();

                    var currentUser = await _userManager.GetUserAsync(User);
                    var currentUserId = currentUser?.Id ?? "system";

                    _logger.LogInformation($"Stage {stage.Stage} for {stage.StationName} deleted by user {currentUserId}");

                    return RedirectToAction(nameof(Index))
                        .WithSuccess("Stage deleted successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error deleting stage: {ex.Message}");
                    ModelState.AddModelError(string.Empty, $"Error deleting stage: {ex.Message}");
                    return View(vm);
                }
            }

            return View(vm);
        }

        private async Task<List<string>> GetAvailableStationsAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var stationNames = new List<string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Any(r => r == SecurityConstants.AdminRoleString))
                {
                    stationNames.Add(user.UserName);
                }
            }

            return stationNames.OrderBy(s => s).ToList();
        }
    }
}