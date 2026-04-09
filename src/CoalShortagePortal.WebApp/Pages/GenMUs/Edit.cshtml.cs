using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Pages.GenMUs
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int? SelectedId { get; set; }

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime DataDate { get; set; }

        [Required]
        [BindProperty]
        [Display(Name = "Daily MUs")]
        [Range(0.01, float.MaxValue, ErrorMessage = "Daily MUs must be greater than 0")]
        public float DailyMUs { get; set; }

        [Required]
        [BindProperty]
        [Range(0.01, float.MaxValue, ErrorMessage = "ExBus must be greater than 0")]
        [Display(Name = "ExBus")]
        public float ExBus { get; set; }

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

        [BindProperty]
        public string StationName { get; set; }

        public List<SelectListItem> StationList { get; set; }
        public List<DailyMUsData> AvailableRecords { get; set; }
        public DailyMUsData SelectedRecord { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // Check if user is admin
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. Only administrators can edit records.";
                return RedirectToPage("./Index");
            }

            await LoadStationListAsync();
            await LoadAvailableRecordsAsync();

            if (id.HasValue)
            {
                var record = await _context.DailyMUsDatas.FindAsync(id.Value);
                if (record == null)
                {
                    TempData["ErrorMessage"] = "Record not found.";
                    return RedirectToPage("./Index");
                }

                SelectedId = record.Id;
                DataDate = record.DataDate;
                DailyMUs = record.DailyMUs;
                ExBus = record.ExBus;
                PeakMW = record.PeakMW;
                PeakMWTime = record.PeakMWTime;
                OffPeakMW = record.OffPeakMW;
                OffPeakMWTime = record.OffPeakMWTime;
                DayPeakMW = record.DayPeakMW;
                DayPeakMWTime = record.DayPeakMWTime;
                MinGeneration = record.MinGeneration;
                MinGenerationTime = record.MinGenerationTime;
                StationName = record.StationName;
                SelectedRecord = record;
            }
            else
            {
                DataDate = DateTime.Now.Date;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            // Check if user is admin
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. Only administrators can edit records.";
                return RedirectToPage("./Index");
            }

            if (!SelectedId.HasValue)
            {
                ModelState.AddModelError("", "Please select a record to edit.");
                await LoadStationListAsync();
                await LoadAvailableRecordsAsync();
                return Page();
            }

            // Validate all fields
            if (DailyMUs <= 0 || ExBus <= 0 || PeakMW <= 0 || OffPeakMW <= 0 || DayPeakMW <= 0 || MinGeneration <= 0)
            {
                ModelState.AddModelError("", "All MW and MU values must be greater than 0.");
                var record = await _context.DailyMUsDatas.FindAsync(SelectedId.Value);
                if (record != null)
                {
                    DataDate = record.DataDate;
                    StationName = record.StationName;
                    SelectedRecord = record;
                }
                await LoadStationListAsync();
                await LoadAvailableRecordsAsync();
                return Page();
            }

            var recordToUpdate = await _context.DailyMUsDatas.FindAsync(SelectedId.Value);
            if (recordToUpdate == null)
            {
                ModelState.AddModelError("", "Record not found.");
                await LoadStationListAsync();
                await LoadAvailableRecordsAsync();
                return Page();
            }

            try
            {
                // Update all editable fields - keep Date and Station unchanged
                recordToUpdate.DailyMUs = DailyMUs;
                recordToUpdate.ExBus = ExBus;
                recordToUpdate.PeakMW = PeakMW;
                recordToUpdate.PeakMWTime = PeakMWTime;
                recordToUpdate.OffPeakMW = OffPeakMW;
                recordToUpdate.OffPeakMWTime = OffPeakMWTime;
                recordToUpdate.DayPeakMW = DayPeakMW;
                recordToUpdate.DayPeakMWTime = DayPeakMWTime;
                recordToUpdate.MinGeneration = MinGeneration;
                recordToUpdate.MinGenerationTime = MinGenerationTime;
                recordToUpdate.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Record updated successfully for <strong>{recordToUpdate.StationName}</strong> on {recordToUpdate.DataDate.ToString("yyyy-MM-dd")}!";
                return RedirectToPage("./Edit");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Please try again.");
                DataDate = recordToUpdate.DataDate;
                StationName = recordToUpdate.StationName;
                SelectedRecord = recordToUpdate;
                await LoadStationListAsync();
                await LoadAvailableRecordsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            // Check if user is admin
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. Only administrators can delete records.";
                return RedirectToPage("./Index");
            }

            if (!SelectedId.HasValue)
            {
                TempData["ErrorMessage"] = "Please select a record to delete.";
                return RedirectToPage("./Edit");
            }

            var record = await _context.DailyMUsDatas.FindAsync(SelectedId.Value);
            if (record != null)
            {
                _context.DailyMUsDatas.Remove(record);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Record for <strong>{record.StationName}</strong> on {record.DataDate.ToString("yyyy-MM-dd")} deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Record not found.";
            }

            return RedirectToPage("./Edit");
        }

        private async Task LoadStationListAsync()
        {
            var stations = await _context.DailyMUsDatas
                .Select(d => d.StationName)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            StationList = stations.Select(s => new SelectListItem
            {
                Value = s,
                Text = s
            }).ToList();
        }

        private async Task LoadAvailableRecordsAsync()
        {
            AvailableRecords = await _context.DailyMUsDatas
                .OrderByDescending(d => d.DataDate)
                .ThenBy(d => d.StationName)
                .Take(100)
                .ToListAsync();
        }
    }
}