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
        public float DailyMUs { get; set; }

        [Required]
        public string StationName { get; set; }

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
            DataDate = DateTime.Now.Date;

            // If user is admin, show all records, otherwise filter by StationName
            //if (User.Identity.Name?.ToLower() == "admin")
            //{
            //    DailyMUsDataList = await _context.DailyMUsDatas
            //        .OrderByDescending(d => d.DataDate)
            //        .ToListAsync();
            //}
            //else
            //{
            //    DailyMUsDataList = await _context.DailyMUsDatas
            //        .Where(co => co.StationName == User.Identity.Name)
            //        .OrderByDescending(d => d.DataDate)
            //        .ToListAsync();
            //}
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
                    $"A record for date {DataDate.ToString("yyyy-MM-dd")} and station '{User.Identity.Name}' already exists.");
                await LoadDataListAsync();
                return Page();
            }

            // Create new entity to save
            var newData = new DailyMUsData
            {
                //DataDate = DataDate.ToUniversalTime(),
                DataDate = DateTime.SpecifyKind(DataDate.Date, DateTimeKind.Utc),
                DailyMUs = DailyMUs,
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
        // Helper method to avoid code duplication
        private async Task LoadDataListAsync()
        {
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
