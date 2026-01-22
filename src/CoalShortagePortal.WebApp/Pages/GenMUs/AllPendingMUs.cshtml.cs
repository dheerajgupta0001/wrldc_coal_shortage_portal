using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Pages.GenMUs
{
    [Authorize]
    public class AllPendingMUsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AllPendingMUsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<PendingMUViewModel> PendingGenerators { get; set; }
        public DateTime YesterdayDate { get; set; }

        [BindProperty]
        public List<MUEntryModel> MUEntries { get; set; }

        public class PendingMUViewModel
        {
            public string StationName { get; set; }
            public string Email { get; set; }
            public bool HasSubmitted { get; set; }
            public float? ExistingMUs { get; set; }
        }

        public class MUEntryModel
        {
            public string StationName { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "Daily MUs must be greater than 0")]
            public float? DailyMUs { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is admin
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. This page is only accessible to administrators.";
                return RedirectToPage("/GenMUs/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            // Get all users (generators)
            var allUsers = await _userManager.Users.ToListAsync();

            // Get all submissions for yesterday
            var yesterdaySubmissions = await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == YesterdayDate)
                .ToListAsync();

            PendingGenerators = new List<PendingMUViewModel>();

            foreach (var user in allUsers)
            {
                // Skip admin user
                if (user.UserName?.ToLower() == "admin")
                    continue;

                var submission = yesterdaySubmissions.FirstOrDefault(s => s.StationName == user.UserName);

                PendingGenerators.Add(new PendingMUViewModel
                {
                    StationName = user.UserName,
                    Email = user.Email,
                    HasSubmitted = submission != null,
                    ExistingMUs = submission?.DailyMUs
                });
            }

            // Sort: pending first, then submitted
            PendingGenerators = PendingGenerators
                .OrderBy(p => p.HasSubmitted)
                .ThenBy(p => p.StationName)
                .ToList();

            // Initialize MUEntries for binding
            MUEntries = PendingGenerators
                .Where(p => !p.HasSubmitted)
                .Select(p => new MUEntryModel { StationName = p.StationName })
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check if user is admin
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. This page is only accessible to administrators.";
                return RedirectToPage("/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var successCount = 0;
            var errorMessages = new List<string>();

            // Filter to only process entries that have values
            var entriesToSave = MUEntries?.Where(e => e.DailyMUs.HasValue && e.DailyMUs > 0).ToList() ?? new List<MUEntryModel>();

            if (!entriesToSave.Any())
            {
                TempData["ErrorMessage"] = "No MU values entered. Please enter at least one value to save.";
                await OnGetAsync();
                return Page();
            }

            foreach (var entry in entriesToSave)
            {
                try
                {
                    // Check if already exists
                    var existing = await _context.DailyMUsDatas
                        .FirstOrDefaultAsync(d => d.DataDate.Date == YesterdayDate
                                                && d.StationName == entry.StationName);

                    if (existing != null)
                    {
                        errorMessages.Add($"{entry.StationName}: Record already exists");
                        continue;
                    }

                    var newData = new DailyMUsData
                    {
                        DataDate = DateTime.SpecifyKind(YesterdayDate, DateTimeKind.Utc),
                        DailyMUs = entry.DailyMUs.Value,
                        StationName = entry.StationName
                    };

                    _context.DailyMUsDatas.Add(newData);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorMessages.Add($"{entry.StationName}: {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Successfully saved {successCount} MU record(s)";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error saving to database: {ex.Message}";
                    await OnGetAsync();
                    return Page();
                }
            }

            if (errorMessages.Any())
            {
                TempData["ErrorMessage"] = string.Join("<br/>", errorMessages);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string stationName)
        {
            // Check if user is admin
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. This page is only accessible to administrators.";
                return RedirectToPage("/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var record = await _context.DailyMUsDatas
                .FirstOrDefaultAsync(d => d.DataDate.Date == YesterdayDate
                                       && d.StationName == stationName);

            if (record != null)
            {
                _context.DailyMUsDatas.Remove(record);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Record for {stationName} deleted successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Record not found";
            }

            return RedirectToPage();
        }
    }
}