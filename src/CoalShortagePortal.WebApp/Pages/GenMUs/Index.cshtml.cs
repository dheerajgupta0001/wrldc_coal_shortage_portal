using Azure.Core;
using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Data;
using CoalShortagePortal.WebApp.Migrations;
using CoalShortagePortal.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Pages.GenMUs
{
    public class IndexModel : PageModel
    {
        [Required]
        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime DataDate { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Daily MUs")]
        [Range(0.01, float.MaxValue, ErrorMessage = "Gross must be greater than 0")]
        public float DailyMUs { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Schedule MUs")]
        [Range(0.01, float.MaxValue, ErrorMessage = "Schedule must be greater than 0")]
        public float ScheduleMUs { get; set; }

        [BindProperty]
        [Display(Name = "ExBus")]
        [Range(0.01, float.MaxValue, ErrorMessage = "ExBus must be greater than 0")]
        public float ExBus { get; set; }

        // PeakMW features added
        [Required]
        [BindProperty]
        [Display(Name = "Peak MW")]
        [Range(1, int.MaxValue, ErrorMessage = "Peak MW must be greater than 0")]
        public int PeakMW { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Peak MW Time")]
        [DataType(DataType.Time)]
        public TimeSpan PeakMWTime { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Off Peak MW")]
        [Range(1, int.MaxValue, ErrorMessage = "Off Peak MW must be greater than 0")]
        public int OffPeakMW { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Off Peak MW Time")]
        [DataType(DataType.Time)]
        public TimeSpan OffPeakMWTime { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Day Peak MW")]
        [Range(1, int.MaxValue, ErrorMessage = "Day Peak MW must be greater than 0")]
        public int DayPeakMW { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Day Peak MW Time")]
        [DataType(DataType.Time)]
        public TimeSpan DayPeakMWTime { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Min Generation")]
        [Range(1, int.MaxValue, ErrorMessage = "Min Generation must be greater than 0")]
        public int MinGeneration { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Min Generation Time")]
        [DataType(DataType.Time)]
        public TimeSpan MinGenerationTime { get; set; }

        // PeakMW features ended

        [Required]
        public string StationName { get; set; }

        // Holds the current user's type fetched from UserDetails
        public string CurrentUserType { get; set; }

        public GenDataDTO GenData { get; set; }
        public UserListVM UserList { get; set; }

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DailyMUsData> DailyMUsDataList { get; set; }

        //public void OnGet()
        public async Task OnGetAsync()
        {
            DataDate = DateTime.Now.AddDays(-1).Date;

            // Set default times based on requirements to 19 & 3
            PeakMWTime = new TimeSpan(19, 0, 0); // 19:00 hrs
            OffPeakMWTime = new TimeSpan(3, 0, 0); // 03:00 hrs
            // Default Time set to 19 & 3

            // Fetch current user's type
            await LoadCurrentUserTypeAsync();

            var query = _context.DailyMUsDatas.AsQueryable();

            // Filter by StationName only if user is not admin
            if (User.Identity.Name?.ToLower() != "admin")
            {
                query = query.Where(co => co.StationName == User.Identity.Name);
            }

            DailyMUsDataList = await query
                .OrderByDescending(d => d.DataDate)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Fetch user type first (needed for validation logic)
            await LoadCurrentUserTypeAsync();

            // If user is NOT ISGS/IPP, clear ScheduleMUs validation
            bool isScheduleUser = CurrentUserType == "ISGS" || CurrentUserType == "IPP";
            if (!isScheduleUser)
            {
                ModelState.Remove("ScheduleMUs");
                ScheduleMUs = 0;
            }

            if (!ModelState.IsValid)
            {
                // Reload the list if validation fails
                await LoadDataListAsync();
                return Page();
            }

            // Check if a record already exists for this date
            var existingRecord = await _context.DailyMUsDatas
                .FirstOrDefaultAsync(d => d.DataDate.Date == DataDate.Date
                                       && d.StationName == User.Identity.Name);

            if (existingRecord != null)
            {
                ModelState.AddModelError("",
                    $"A record for date {DataDate.ToString("yyyy-MM-dd")} and Generating Station '{User.Identity.Name}' already exists.");
                await LoadDataListAsync();
                return Page();
            }

            // Create new entity to save
            var newData = new DailyMUsData
            {
                //DataDate = DataDate.ToUniversalTime(),
                DataDate = DateTime.SpecifyKind(DataDate.Date, DateTimeKind.Utc),
                DailyMUs = DailyMUs,
                ScheduleMUs = isScheduleUser ? ScheduleMUs : 0,
                ExBus = ExBus,
                PeakMW = PeakMW,
                PeakMWTime = PeakMWTime,
                OffPeakMW = OffPeakMW,
                OffPeakMWTime = OffPeakMWTime,
                DayPeakMW = DayPeakMW,
                DayPeakMWTime = DayPeakMWTime,
                MinGeneration = MinGeneration,
                MinGenerationTime = MinGenerationTime,
                StationName = User.Identity.Name
                // Add other properties as needed
            };

            try
            {
                _context.DailyMUsDatas.Add(newData);
                await _context.SaveChangesAsync();

                // Success message (optional)
                TempData["SuccessMessage"] = "Data saved successfully!";

                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                // Handle database constraint violations
                if (ex.InnerException?.Message.Contains("IX_DailyMUsDatas_DataDate_StationName") == true)
                {
                    ModelState.AddModelError("",
                        $"A record for date {DataDate.ToString("yyyy-MM-dd")} and Generating Station '{User.Identity.Name}' already exists.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
                await LoadDataListAsync();
                return Page();
            }
        }
        private async Task LoadCurrentUserTypeAsync()
        {
            // Step 1: Get the AspNetUsers.Id (GUID) for the currently logged-in username
            var aspNetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (aspNetUser == null)
            {
                CurrentUserType = "";
                return;
            }

            // Step 2: Use that GUID to find the matching UserDetails row
            var userDetail = await _context.UserDetails
                .FirstOrDefaultAsync(u => u.UserId == aspNetUser.Id);

            CurrentUserType = userDetail?.UserType ?? "";
        }

        // Helper method to avoid code duplication
        private async Task LoadDataListAsync()
        {
            // Re-load user type if not already set
            if (string.IsNullOrEmpty(CurrentUserType))
            {
                await LoadCurrentUserTypeAsync();
            }

            var query = _context.DailyMUsDatas.AsQueryable();

            if (User.Identity.Name?.ToLower() != "admin")
            {
                query = query.Where(co => co.StationName == User.Identity.Name);
            }

            DailyMUsDataList = await query
                .OrderByDescending(d => d.DataDate)
                .ToListAsync();
        }
    }
}
