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

        [BindProperty]
        [Range(0.01, float.MaxValue, ErrorMessage = "ExBus must be greater than 0")]
        [Display(Name = "ExBus")]
        public float ExBus { get; set; }

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

            // Only validate DailyMUs & EX Bus
            if (DailyMUs <= 0 || ExBus <=0)
            {
                ModelState.AddModelError("ExBus", "Both Daily MUs & ExBus must be greater than 0.");
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
                // Only update DailyMUs - keep Date and Station unchanged
                recordToUpdate.DailyMUs = DailyMUs;
                recordToUpdate.ExBus = ExBus;
                recordToUpdate.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"MU value updated successfully for Generating Station {recordToUpdate.StationName} on {recordToUpdate.DataDate.ToString("yyyy-MM-dd")}!";
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
                TempData["SuccessMessage"] = $"Record for Generating Station {record.StationName} on {record.DataDate.ToString("yyyy-MM-dd")} deleted successfully!";
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