using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Pages.GenMUs
{
    [Authorize]
    public class AllPendingMUsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AllPendingMUsModel> _logger;

        public AllPendingMUsModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<AllPendingMUsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<PendingMUViewModel> PendingGenerators { get; set; }
        public DateTime YesterdayDate { get; set; }
        public List<string> EditingStations { get; set; } = new List<string>();

        [BindProperty]
        public List<MUEntryModel> MUEntries { get; set; }

        public class PendingMUViewModel
        {
            public string StationName { get; set; }
            public string Email { get; set; }
            public bool HasSubmitted { get; set; }
            public string UserType { get; set; }
            public float? ExistingMUs { get; set; }
            public float? ExistingScheduleMUs { get; set; }
            public float? ExistingExBus { get; set; }
            public int? ExistingPeakMW { get; set; }
            public TimeSpan? ExistingPeakMWTime { get; set; }
            public int? ExistingOffPeakMW { get; set; }
            public TimeSpan? ExistingOffPeakMWTime { get; set; }
            public int? ExistingDayPeakMW { get; set; }
            public TimeSpan? ExistingDayPeakMWTime { get; set; }
            public int? ExistingMinGeneration { get; set; }
            public TimeSpan? ExistingMinGenerationTime { get; set; }
            public bool IsScheduleUser => UserType == "ISGS" || UserType == "IPP";
        }

        public class MUEntryModel
        {
            public string StationName { get; set; }

            // ✅ UserType from hidden field — will be re-validated from DB in OnPostAsync
            public string UserType { get; set; }

            // ✅ IsEditMode from hidden field — will be re-determined from DB in OnPostAsync
            public bool IsEditMode { get; set; }

            [Range(0.01, 99999.99,
                ErrorMessage = "Gross must be between 0.01 and 99,999.99")]
            public float? DailyMUs { get; set; }

            [Range(0.01, 99999.99,
                ErrorMessage = "Schedule must be between 0.01 and 99,999.99")]
            public float? ScheduleMUs { get; set; }

            [Range(0.01, 99999.99,
                ErrorMessage = "ExBus must be between 0.01 and 99,999.99")]
            public float? ExBus { get; set; }

            [Range(1, 99999,
                ErrorMessage = "Peak MW must be between 1 and 99,999")]
            public int? PeakMW { get; set; }

            [DataType(DataType.Time)]
            public TimeSpan? PeakMWTime { get; set; }

            [Range(1, 99999,
                ErrorMessage = "Off Peak MW must be between 1 and 99,999")]
            public int? OffPeakMW { get; set; }

            [DataType(DataType.Time)]
            public TimeSpan? OffPeakMWTime { get; set; }

            [Range(1, 99999,
                ErrorMessage = "Day Peak MW must be between 1 and 99,999")]
            public int? DayPeakMW { get; set; }

            [DataType(DataType.Time)]
            public TimeSpan? DayPeakMWTime { get; set; }

            [Range(1, 99999,
                ErrorMessage = "Min Generation must be between 1 and 99,999")]
            public int? MinGeneration { get; set; }

            [DataType(DataType.Time)]
            public TimeSpan? MinGenerationTime { get; set; }

            public bool IsScheduleUser => UserType == "ISGS" || UserType == "IPP";
        }

        // ============================================================
        // GET
        // ============================================================
        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] =
                    "Access denied. This page is only accessible to administrators.";
                return RedirectToPage("/GenMUs/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            // ✅ Restore editing stations from TempData using Peek (keeps it alive)
            if (TempData.Peek("EditingStations") is string editJson
                && !string.IsNullOrEmpty(editJson))
            {
                EditingStations = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(editJson);
                TempData.Keep("EditingStations");
            }

            await LoadPageDataAsync();
            return Page();
        }

        // ============================================================
        // GET: Toggle Edit
        // ============================================================
        public IActionResult OnGetToggleEdit(string stationName)
        {
            if (User.Identity.Name?.ToLower() != "admin")
                return RedirectToPage("/GenMUs/Index");

            // ✅ Validate stationName is not empty/null
            if (string.IsNullOrWhiteSpace(stationName))
                return RedirectToPage("./AllPendingMUs");

            var editing = new List<string>();
            if (TempData["EditingStations"] is string existing
                && !string.IsNullOrEmpty(existing))
                editing = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(existing);

            if (!editing.Contains(stationName))
                editing.Add(stationName);

            // ✅ Persist via TempData then redirect to clean URL — no query string
            TempData["EditingStations"] = System.Text.Json.JsonSerializer
                .Serialize(editing);

            return RedirectToPage("./AllPendingMUs");
        }

        // ============================================================
        // POST: Save All
        // ============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage("/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var successCount = 0;
            var editCount = 0;
            var errorMessages = new List<string>();

            var entriesToProcess = MUEntries?
                .Where(e => e.DailyMUs.HasValue && e.DailyMUs > 0)
                .ToList() ?? new List<MUEntryModel>();

            if (!entriesToProcess.Any())
            {
                TempData["ErrorMessage"] =
                    "No MU values entered. Please enter at least one value to save.";
                await LoadPageDataAsync();
                return Page();
            }

            foreach (var entry in entriesToProcess)
            {
                try
                {
                    // ✅ SECURITY FIX 1: Validate station exists in Identity — never trust hidden field
                    var stationUser = await _userManager
                        .FindByNameAsync(entry.StationName);
                    if (stationUser == null)
                    {
                        _logger.LogWarning(
                            "Invalid station name submitted by admin: {Station}",
                            entry.StationName);
                        errorMessages.Add(
                            $"Invalid station: record skipped.");
                        continue;
                    }

                    // ✅ SECURITY FIX 2: Get UserType from DB — never trust hidden field
                    var dbUserType = await _context.UserDetails
                        .Where(ud => ud.UserId == stationUser.Id)
                        .Select(ud => ud.UserType)
                        .FirstOrDefaultAsync() ?? "";

                    bool isScheduleUser =
                        dbUserType == "ISGS" || dbUserType == "IPP";

                    // Validate ExBus
                    if (entry.ExBus.HasValue && entry.ExBus <= 0)
                    {
                        errorMessages.Add(
                            $"{entry.StationName}: ExBus must be greater than 0");
                        continue;
                    }

                    // Validate ScheduleMUs for ISGS/IPP
                    if (isScheduleUser &&
                        (!entry.ScheduleMUs.HasValue || entry.ScheduleMUs <= 0))
                    {
                        errorMessages.Add(
                            $"{entry.StationName}: Schedule MUs required " +
                            $"for {dbUserType} stations.");
                        continue;
                    }

                    // ✅ SECURITY FIX 3: Determine edit mode from DB — never trust hidden field
                    var existing = await _context.DailyMUsDatas
                        .FirstOrDefaultAsync(d =>
                            d.DataDate.Date == YesterdayDate
                            && d.StationName == entry.StationName);

                    bool isActuallyEditMode = existing != null;

                    if (isActuallyEditMode)
                    {
                        // UPDATE existing record
                        existing.DailyMUs = entry.DailyMUs.Value;
                        existing.ScheduleMUs = isScheduleUser
                            ? (entry.ScheduleMUs ?? 0f) : 0f;
                        existing.ExBus = entry.ExBus.Value;
                        existing.PeakMW = entry.PeakMW.Value;
                        existing.PeakMWTime =
                            entry.PeakMWTime ?? new TimeSpan(19, 0, 0);
                        existing.OffPeakMW = entry.OffPeakMW.Value;
                        existing.OffPeakMWTime =
                            entry.OffPeakMWTime ?? new TimeSpan(3, 0, 0);
                        existing.DayPeakMW = entry.DayPeakMW.Value;
                        existing.DayPeakMWTime = entry.DayPeakMWTime.Value;
                        existing.MinGeneration = entry.MinGeneration.Value;
                        existing.MinGenerationTime = entry.MinGenerationTime.Value;
                        existing.LastModified = DateTime.UtcNow;
                        editCount++;
                    }
                    else
                    {
                        // INSERT new record
                        var newData = new DailyMUsData
                        {
                            DataDate = DateTime.SpecifyKind(
                                YesterdayDate, DateTimeKind.Utc),
                            DailyMUs = entry.DailyMUs.Value,
                            ScheduleMUs = isScheduleUser
                                ? (entry.ScheduleMUs ?? 0f) : 0f,
                            ExBus = entry.ExBus.Value,
                            PeakMW = entry.PeakMW.Value,
                            PeakMWTime =
                                entry.PeakMWTime ?? new TimeSpan(19, 0, 0),
                            OffPeakMW = entry.OffPeakMW.Value,
                            OffPeakMWTime =
                                entry.OffPeakMWTime ?? new TimeSpan(3, 0, 0),
                            DayPeakMW = entry.DayPeakMW.Value,
                            DayPeakMWTime = entry.DayPeakMWTime.Value,
                            MinGeneration = entry.MinGeneration.Value,
                            MinGenerationTime = entry.MinGenerationTime.Value,
                            StationName = entry.StationName
                        };
                        _context.DailyMUsDatas.Add(newData);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    // ✅ SECURITY FIX 4: Log full error internally, show generic message
                    _logger.LogError(ex,
                        "Error processing MU entry for station {Station}",
                        entry.StationName);
                    errorMessages.Add(
                        $"{entry.StationName}: Failed to process. Please try again.");
                }
            }

            if (successCount > 0 || editCount > 0)
            {
                try
                {
                    await _context.SaveChangesAsync();

                    // ✅ Clear edit mode after successful save
                    TempData.Remove("EditingStations");

                    var parts = new List<string>();
                    if (successCount > 0)
                        parts.Add($"{successCount} new record(s) saved");
                    if (editCount > 0)
                        parts.Add($"{editCount} record(s) updated");
                    TempData["SuccessMessage"] =
                        string.Join(", ", parts) + " successfully!";
                }
                catch (Exception ex)
                {
                    // ✅ SECURITY FIX 4: Generic error message to user
                    _logger.LogError(ex,
                        "Database error saving MU data by admin {Admin}",
                        User.Identity.Name);
                    TempData["ErrorMessage"] =
                        "An error occurred while saving. Please try again or contact support.";
                    await LoadPageDataAsync();
                    return Page();
                }
            }

            if (errorMessages.Any())
                TempData["ErrorMessage"] = string.Join("<br/>", errorMessages);

            return RedirectToPage();
        }

        // ============================================================
        // POST: Delete
        // ============================================================
        public async Task<IActionResult> OnPostDeleteAsync(string stationName)
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage("/Index");
            }

            // ✅ Validate stationName
            if (string.IsNullOrWhiteSpace(stationName))
            {
                TempData["ErrorMessage"] = "Invalid station name.";
                return RedirectToPage();
            }

            if (stationName.StartsWith("wr_"))
            {
                TempData["ErrorMessage"] =
                    "Cannot delete records for stations starting with 'wr_'";
                return RedirectToPage();
            }

            // ✅ Verify station exists in Identity before deleting
            var stationUser = await _userManager.FindByNameAsync(stationName);
            if (stationUser == null)
            {
                _logger.LogWarning(
                    "Delete attempt for non-existent station: {Station} by {Admin}",
                    stationName, User.Identity.Name);
                TempData["ErrorMessage"] = "Station not found.";
                return RedirectToPage();
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var record = await _context.DailyMUsDatas
                .FirstOrDefaultAsync(d =>
                    d.DataDate.Date == YesterdayDate
                    && d.StationName == stationName);

            if (record != null)
            {
                _context.DailyMUsDatas.Remove(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Record deleted for station {Station} by admin {Admin}",
                    stationName, User.Identity.Name);
                TempData["SuccessMessage"] =
                    $"Record for station deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Record not found.";
            }

            return RedirectToPage();
        }

        // ============================================================
        // GET: Export Excel
        // ============================================================
        [Authorize]
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                _logger.LogWarning(
                    "Unauthorized export attempt by {User}",
                    User.Identity.Name);
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage();
            }

            var yesterday = DateTime.Now.AddDays(-1).Date;

            var allUsers = await _userManager.Users
                .Where(u => !u.UserName.StartsWith("wr_")
                         && u.UserName.ToLower() != "admin")
                .ToListAsync();

            var submissions = await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == yesterday
                         && !d.StationName.StartsWith("wr_"))
                .OrderBy(d => d.StationName)
                .ToListAsync();

            var pendingStations = allUsers
                .Where(u => !submissions.Any(s => s.StationName == u.UserName))
                .ToList();

            //if (pendingStations.Any())
            //{
            //    TempData["ErrorMessage"] =
            //        $"Cannot export — {pendingStations.Count} " +
            //        $"station(s) have not submitted yet.";
            //    return RedirectToPage();
            //}

            // Get UserTypes for all stations
            var allUserIds = allUsers.Select(u => u.Id).ToList();
            var userTypeMap = await _context.UserDetails
                .Where(ud => allUserIds.Contains(ud.UserId))
                .ToDictionaryAsync(ud => ud.UserId, ud => ud.UserType);

            var userNameToTypeMap = allUsers.ToDictionary(
                u => u.UserName,
                u => userTypeMap.TryGetValue(u.Id, out var ut) ? ut : "");

            ExcelPackage.License.SetNonCommercialPersonal("CoalShortagePortal");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("MU Data");

            int totalCols = submissions.Count + 1;

            // Row 1: Title
            ws.Cells[1, 1, 1, totalCols].Merge = true;
            ws.Cells[1, 1].Value =
                $"Daily MU & ExBus Data — {yesterday:dd-MMM-yyyy}";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 13;
            ws.Cells[1, 1].Style.HorizontalAlignment =
                ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.VerticalAlignment =
                ExcelVerticalAlignment.Center;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(
                Color.FromArgb(102, 126, 234));
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Row(1).Height = 26;

            // Row 2: Station names
            ws.Cells[2, 1].Value = "Station";
            StyleLabelCell(ws.Cells[2, 1]);
            for (int i = 0; i < submissions.Count; i++)
            {
                var cell = ws.Cells[2, i + 2];
                cell.Value = submissions[i].StationName;
                cell.Style.Font.Bold = true;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(
                    Color.FromArgb(220, 220, 245));
            }

            // Row 3: Daily MUs
            ws.Cells[3, 1].Value = "Daily MUs";
            StyleLabelCell(ws.Cells[3, 1]);
            for (int i = 0; i < submissions.Count; i++)
            {
                var cell = ws.Cells[3, i + 2];
                cell.Value = submissions[i].DailyMUs;
                cell.Style.Numberformat.Format = "#,##0.00";
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(
                    Color.FromArgb(255, 253, 231));
            }

            // Row 4: Schedule MUs — ISGS/IPP only
            ws.Cells[4, 1].Value = "Schedule MUs";
            StyleLabelCell(ws.Cells[4, 1]);
            for (int i = 0; i < submissions.Count; i++)
            {
                var cell = ws.Cells[4, i + 2];
                var stationName = submissions[i].StationName;
                var ut = userNameToTypeMap.TryGetValue(stationName, out var t)
                    ? t : "";
                bool isIsgsIpp = ut == "ISGS" || ut == "IPP";

                if (isIsgsIpp)
                {
                    cell.Value = submissions[i].ScheduleMUs;
                    cell.Style.Numberformat.Format = "#,##0.00";
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(
                        Color.FromArgb(243, 229, 245));
                }
                else
                {
                    cell.Value = "N/A";
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(
                        Color.FromArgb(240, 240, 240));
                    cell.Style.Font.Color.SetColor(Color.Gray);
                }
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Row 5: ExBus
            ws.Cells[5, 1].Value = "ExBus";
            StyleLabelCell(ws.Cells[5, 1]);
            for (int i = 0; i < submissions.Count; i++)
            {
                var cell = ws.Cells[5, i + 2];
                cell.Value = submissions[i].ExBus;
                cell.Style.Numberformat.Format = "#,##0.00";
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(
                    Color.FromArgb(232, 245, 233));
            }

            var dataBlock = ws.Cells[2, 1, 5, totalCols];
            dataBlock.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataBlock.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataBlock.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataBlock.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataBlock.Style.Border.BorderAround(ExcelBorderStyle.Medium);

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            ws.Column(1).Width = 14;
            for (int c = 2; c <= totalCols; c++)
                ws.Column(c).Width = Math.Max(ws.Column(c).Width, 14);

            for (int r = 1; r <= 5; r++)
                ws.Row(r).Height = 20;

            var bytes = package.GetAsByteArray();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"MU_Data_{yesterday:yyyyMMdd}.xlsx");
        }

        // ============================================================
        // Private Helpers
        // ============================================================
        private async Task LoadPageDataAsync()
        {
            var allUsers = await _userManager.Users
                .Where(u => !u.UserName.StartsWith("wr_"))
                .ToListAsync();

            var yesterdaySubmissions = await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == YesterdayDate
                         && !d.StationName.StartsWith("wr_"))
                .ToListAsync();

            var allUserIds = allUsers.Select(u => u.Id).ToList();
            var userDetailsMap = await _context.UserDetails
                .Where(ud => allUserIds.Contains(ud.UserId))
                .ToDictionaryAsync(ud => ud.UserId, ud => ud.UserType);

            PendingGenerators = new List<PendingMUViewModel>();

            foreach (var user in allUsers)
            {
                if (user.UserName?.ToLower() == "admin") continue;

                var submission = yesterdaySubmissions
                    .FirstOrDefault(s => s.StationName == user.UserName);
                userDetailsMap.TryGetValue(user.Id, out var userType);

                PendingGenerators.Add(new PendingMUViewModel
                {
                    StationName = user.UserName,
                    Email = user.Email,
                    HasSubmitted = submission != null,
                    UserType = userType ?? "",
                    ExistingMUs = submission?.DailyMUs,
                    ExistingScheduleMUs = submission?.ScheduleMUs,
                    ExistingExBus = submission?.ExBus,
                    ExistingPeakMW = submission?.PeakMW,
                    ExistingPeakMWTime = submission?.PeakMWTime,
                    ExistingOffPeakMW = submission?.OffPeakMW,
                    ExistingOffPeakMWTime = submission?.OffPeakMWTime,
                    ExistingDayPeakMW = submission?.DayPeakMW,
                    ExistingDayPeakMWTime = submission?.DayPeakMWTime,
                    ExistingMinGeneration = submission?.MinGeneration,
                    ExistingMinGenerationTime = submission?.MinGenerationTime
                });
            }

            PendingGenerators = PendingGenerators
                .OrderBy(p => p.HasSubmitted)
                .ThenBy(p => p.StationName)
                .ToList();

            MUEntries = new List<MUEntryModel>();

            foreach (var p in PendingGenerators)
            {
                bool isEditing = EditingStations.Contains(p.StationName);

                if (!p.HasSubmitted)
                {
                    MUEntries.Add(new MUEntryModel
                    {
                        StationName = p.StationName,
                        UserType = p.UserType,
                        IsEditMode = false,
                        PeakMWTime = new TimeSpan(19, 0, 0),
                        OffPeakMWTime = new TimeSpan(3, 0, 0)
                    });
                }
                else if (isEditing)
                {
                    MUEntries.Add(new MUEntryModel
                    {
                        StationName = p.StationName,
                        UserType = p.UserType,
                        IsEditMode = true,
                        DailyMUs = p.ExistingMUs,
                        ScheduleMUs = p.ExistingScheduleMUs,
                        ExBus = p.ExistingExBus,
                        PeakMW = p.ExistingPeakMW,
                        PeakMWTime = p.ExistingPeakMWTime,
                        OffPeakMW = p.ExistingOffPeakMW,
                        OffPeakMWTime = p.ExistingOffPeakMWTime,
                        DayPeakMW = p.ExistingDayPeakMW,
                        DayPeakMWTime = p.ExistingDayPeakMWTime,
                        MinGeneration = p.ExistingMinGeneration,
                        MinGenerationTime = p.ExistingMinGenerationTime
                    });
                }
            }
        }

        private static void StyleLabelCell(ExcelRange cell)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 30, 60));
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
    }
}