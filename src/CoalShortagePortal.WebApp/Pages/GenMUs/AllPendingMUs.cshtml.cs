using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Core.Interfaces.ReportingData;
using CoalShortagePortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Pages.GenMUs
{
    [Authorize]
    public class AllPendingMUsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AllPendingMUsModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IExBusGenMUsExportService _exportService;

        public AllPendingMUsModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<AllPendingMUsModel> logger,
            IConfiguration configuration,
            IExBusGenMUsExportService exportService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _exportService = exportService;
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

            public string UserType { get; set; }

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

        public class ScadaCellMappingModel
        {
            public int Id { get; set; }
            public string GeneratingStationName { get; set; }
            public string StationTypeName { get; set; }
            public int? Fuel { get; set; }
            public string FuelType { get; set; }
            public string CellReference { get; set; }
        }

        // ============================================================
        // GET
        // ============================================================
        public async Task<IActionResult> OnGetAsync()
        {
            //if (User.Identity.Name?.ToLower() != "admin")
            //{
            //    TempData["ErrorMessage"] =
            //        "Access denied. This page is only accessible to administrators.";
            //    return RedirectToPage("/GenMUs/Index");
            //}
            var currentUser = await GetCurrentUserInfo();

            if (!currentUser.IsAdmin && !currentUser.IsSldc)
            {
                TempData["ErrorMessage"] =
                    "Access denied.";

                return RedirectToPage("/GenMUs/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

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
        //public IActionResult OnGetToggleEdit(string stationName)
        public async Task<IActionResult> OnGetToggleEdit(string stationName)
        {
            //if (User.Identity.Name?.ToLower() != "admin")
            //    return RedirectToPage("/GenMUs/Index");
            var currentUser = await GetCurrentUserInfo();

            if (!currentUser.IsAdmin && !currentUser.IsSldc)
            {
                TempData["ErrorMessage"] =
                    "Access denied.";

                return RedirectToPage("/GenMUs/Index");
            }

            if (string.IsNullOrWhiteSpace(stationName))
                return RedirectToPage("./AllPendingMUs");

            var editing = new List<string>();

            if (TempData["EditingStations"] is string existing
                && !string.IsNullOrEmpty(existing))
            {
                editing = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(existing);
            }

            if (!editing.Contains(stationName))
                editing.Add(stationName);

            TempData["EditingStations"] = System.Text.Json.JsonSerializer
                .Serialize(editing);

            return RedirectToPage("./AllPendingMUs");
        }

        // ============================================================
        // POST: Save All
        // ============================================================
        public async Task<IActionResult> OnPostAsync()
        {
            //if (User.Identity.Name?.ToLower() != "admin")
            //{
            //    TempData["ErrorMessage"] = "Access denied.";
            //    return RedirectToPage("/Index");
            //}
            var currentUser = await GetCurrentUserInfo();

            if (!currentUser.IsAdmin && !currentUser.IsSldc)
            {
                TempData["ErrorMessage"] =
                    "Access denied.";

                return RedirectToPage("/GenMUs/Index");
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
                    var stationUser = await _userManager
                        .FindByNameAsync(entry.StationName);

                    if (stationUser == null)
                    {
                        _logger.LogWarning(
                            "Invalid station name submitted by admin: {Station}",
                            entry.StationName);

                        errorMessages.Add("Invalid station: record skipped.");
                        continue;
                    }

                    var dbUserType = await _context.UserDetails
                        .Where(ud => ud.UserId == stationUser.Id)
                        .Select(ud => ud.UserType)
                        .FirstOrDefaultAsync() ?? "";

                    bool isScheduleUser =
                        dbUserType == "ISGS" || dbUserType == "IPP";

                    if (entry.ExBus.HasValue && entry.ExBus <= 0)
                    {
                        errorMessages.Add(
                            $"{entry.StationName}: ExBus must be greater than 0");
                        continue;
                    }

                    if (isScheduleUser &&
                        (!entry.ScheduleMUs.HasValue || entry.ScheduleMUs <= 0))
                    {
                        errorMessages.Add(
                            $"{entry.StationName}: Schedule MUs required for {dbUserType} stations.");
                        continue;
                    }

                    var existing = await _context.DailyMUsDatas
                        .FirstOrDefaultAsync(d =>
                            d.DataDate.Date == YesterdayDate
                            && d.StationName == entry.StationName);

                    bool isActuallyEditMode = existing != null;

                    if (isActuallyEditMode)
                    {
                        existing.DailyMUs = entry.DailyMUs.Value;
                        existing.ScheduleMUs = isScheduleUser
                            ? (entry.ScheduleMUs ?? 0f)
                            : 0f;

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
                        var newData = new DailyMUsData
                        {
                            DataDate = DateTime.SpecifyKind(
                                YesterdayDate,
                                DateTimeKind.Utc),

                            DailyMUs = entry.DailyMUs.Value,

                            ScheduleMUs = isScheduleUser
                                ? (entry.ScheduleMUs ?? 0f)
                                : 0f,

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
                    _logger.LogError(
                        ex,
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
                    _logger.LogError(
                        ex,
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
            //if (User.Identity.Name?.ToLower() != "admin")
            //{
            //    TempData["ErrorMessage"] = "Access denied.";
            //    return RedirectToPage("/Index");
            //}
            var currentUser = await GetCurrentUserInfo();

            if (!currentUser.IsAdmin && !currentUser.IsSldc)
            {
                TempData["ErrorMessage"] =
                    "Access denied.";

                return RedirectToPage("/GenMUs/Index");
            }

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

            var stationUser = await _userManager.FindByNameAsync(stationName);

            if (stationUser == null)
            {
                _logger.LogWarning(
                    "Delete attempt for non-existent station: {Station} by {Admin}",
                    stationName,
                    User.Identity.Name);

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
                    stationName,
                    User.Identity.Name);

                TempData["SuccessMessage"] =
                    "Record for station deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Record not found.";
            }

            return RedirectToPage();
        }

        // ============================================================
        // GET: Export ExBus Excel
        // File name example: ExBus_22_07_2026.xlsx
        // ============================================================
        [Authorize]
        public async Task<IActionResult> OnGetExportExBusExcelAsync()
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                _logger.LogWarning(
                    "Unauthorized ExBus export attempt by {User}",
                    User.Identity.Name);

                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage();
            }

            var yesterday = DateTime.Now.AddDays(-1).Date;

            var submissions = await GetYesterdaySubmissionsAsync(yesterday);

            var scadaMappings = await GetScadaCellMappingsAsync();

            ExcelPackage.License.SetNonCommercialPersonal("CoalShortagePortal");

            var bytes = GenerateCellReferenceExcel(
                submissions: submissions,
                scadaMappings: scadaMappings,
                sheetName: "ExBus",
                valueSelector: d => d.ExBus,
                valueRowColor: Color.FromArgb(232, 245, 233));

            string fileName = $"ExBus_{yesterday:dd_MM_yyyy}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ============================================================
        // GET: Export GenMU Excel
        // File name example: GenMU_22_07_2026.xlsx
        // ============================================================
        [Authorize]
        public async Task<IActionResult> OnGetExportGenMuExcelAsync()
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                _logger.LogWarning(
                    "Unauthorized GenMU export attempt by {User}",
                    User.Identity.Name);

                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage();
            }

            var yesterday = DateTime.Now.AddDays(-1).Date;

            var submissions = await GetYesterdaySubmissionsAsync(yesterday);

            var scadaMappings = await GetScadaCellMappingsAsync();

            ExcelPackage.License.SetNonCommercialPersonal("CoalShortagePortal");

            var bytes = GenerateCellReferenceExcel(
                submissions: submissions,
                scadaMappings: scadaMappings,
                sheetName: "GenMU",
                valueSelector: d => d.DailyMUs,
                valueRowColor: Color.FromArgb(255, 253, 231));

            string fileName = $"GenMU_{yesterday:dd_MM_yyyy}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ============================================================
        // Optional old handler kept for safety
        // If old Export button still calls ?handler=ExportExcel,
        // this returns GenMU only.
        // Prefer using JS below to call both new handlers.
        // ============================================================
        [Authorize]
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            return await OnGetExportGenMuExcelAsync();
        }

        // ============================================================
        // Private Helpers
        // ============================================================
        private async Task LoadPageDataAsync()
        {
            //var allUsers = await _userManager.Users
            //    .Where(u => !u.UserName.StartsWith("wr_"))
            //    .ToListAsync();

            var currentUser = await GetCurrentUserInfo();

            List<IdentityUser> allUsers;

            if (currentUser.IsAdmin)
            {
                allUsers = await _userManager.Users
                    .Where(u => !u.UserName.StartsWith("wr_"))
                    .ToListAsync();
            }
            else
            {
                var allowedUserIds = await _context.UserDetails
                    .Where(x =>
                        x.UserType == "State_Gen"
                        && x.State == currentUser.State
                        && !x.IsSldc)
                    .Select(x => x.UserId)
                    .ToListAsync();

                allUsers = await _userManager.Users
                    .Where(x => allowedUserIds.Contains(x.Id))
                    .ToListAsync();
            }

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
                if (user.UserName?.ToLower() == "admin")
                    continue;

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

        private async Task<List<DailyMUsData>> GetYesterdaySubmissionsAsync(DateTime yesterday)
        {
            return await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == yesterday
                         && !d.StationName.StartsWith("wr_"))
                .OrderBy(d => d.StationName)
                .ToListAsync();
        }

        private byte[] GenerateCellReferenceExcel(
            List<DailyMUsData> submissions,
            List<ScadaCellMappingModel> scadaMappings,
            string sheetName,
            Func<DailyMUsData, float?> valueSelector,
            Color valueRowColor)
        {
            using var package = new ExcelPackage();

            var ws = package.Workbook.Worksheets.Add(sheetName);

            var submissionMap = submissions
                .GroupBy(s => NormalizeStationName(s.StationName))
                .ToDictionary(
                    g => g.Key,
                    g => g.First());

            int maxColumnUsed = 1;

            foreach (var mapping in scadaMappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.GeneratingStationName))
                    continue;

                if (string.IsNullOrWhiteSpace(mapping.CellReference))
                    continue;

                string stationKey =
                    NormalizeStationName(mapping.GeneratingStationName);

                submissionMap.TryGetValue(
                    stationKey,
                    out var submission);

                string columnLetters =
                    ExtractColumnLetters(mapping.CellReference);

                if (string.IsNullOrWhiteSpace(columnLetters))
                    continue;

                int columnNumber =
                    ExcelColumnNameToNumber(columnLetters);

                if (columnNumber <= 0)
                    continue;

                maxColumnUsed = Math.Max(maxColumnUsed, columnNumber);

                // Row 1: Station name at Oracle CELL_REFERENCE column
                var stationCell = ws.Cells[1, columnNumber];
                stationCell.Value = mapping.GeneratingStationName;
                stationCell.Style.Font.Bold = true;
                stationCell.Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                stationCell.Style.VerticalAlignment =
                    ExcelVerticalAlignment.Center;
                stationCell.Style.Fill.PatternType =
                    ExcelFillStyle.Solid;
                stationCell.Style.Fill.BackgroundColor.SetColor(
                    Color.FromArgb(220, 220, 245));
                stationCell.Style.Border.BorderAround(
                    ExcelBorderStyle.Thin);

                // Row 2: GenMU / ExBus value at same Oracle CELL_REFERENCE column
                var valueCell = ws.Cells[2, columnNumber];

                if (submission != null)
                {
                    var mappedValue = valueSelector(submission);

                    if (mappedValue.HasValue)
                    {
                        valueCell.Value = mappedValue.Value;
                    }
                    else
                    {
                        valueCell.Value = null; // leave blank if value itself is null
                    }
                }
                else
                {
                    valueCell.Value = null; // leave blank if station has no portal data
                }

                valueCell.Style.Numberformat.Format = "#,##0.00";
                valueCell.Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                valueCell.Style.VerticalAlignment =
                    ExcelVerticalAlignment.Center;
                valueCell.Style.Fill.PatternType =
                    ExcelFillStyle.Solid;
                valueCell.Style.Fill.BackgroundColor.SetColor(valueRowColor);
                valueCell.Style.Border.BorderAround(
                    ExcelBorderStyle.Thin);
            }

            if (ws.Dimension != null)
            {
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            for (int c = 1; c <= maxColumnUsed; c++)
            {
                ws.Column(c).Width = Math.Max(ws.Column(c).Width, 12);
            }

            ws.Row(1).Height = 22;
            ws.Row(2).Height = 22;

            return package.GetAsByteArray();
        }
        private static string NormalizeStationName(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
                return string.Empty;

            return stationName
                .Trim()
                .ToUpperInvariant();
        }

        private static string ExtractColumnLetters(string cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
                return string.Empty;

            var cleaned = cellReference
                .Trim()
                .Replace("$", "")
                .ToUpperInvariant();

            var match = Regex.Match(cleaned, "^[A-Z]+");

            return match.Success ? match.Value : string.Empty;
        }

        private static int ExcelColumnNameToNumber(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return 0;

            columnName = columnName.Trim().ToUpperInvariant();

            int sum = 0;

            foreach (char c in columnName)
            {
                if (c < 'A' || c > 'Z')
                    return 0;

                sum *= 26;
                sum += c - 'A' + 1;
            }

            return sum;
        }

        private async Task<List<ScadaCellMappingModel>> GetScadaCellMappingsAsync()
        {
            var result = new List<ScadaCellMappingModel>();

            var connectionString =
                _configuration.GetConnectionString("ReportingConnection");

            const string query = @"
SELECT
    gs.ID,
    gs.GENERATING_STATION_NAME,
    gst.NAME AS STATION_TYPE_NAME,
    gs.FUEL,
    f.TYPE AS FUEL_TYPE,
    scd.CELL_REFERENCE
FROM REPORTING_WEB_UI_UAT.GENERATING_STATION gs
INNER JOIN REPORTING_WEB_UI_UAT.SCADA_CONFIG_DETAILS scd
    ON scd.ELMENT_ID = gs.ID
LEFT JOIN REPORTING_WEB_UI_UAT.FUEL f
    ON f.ID = gs.FUEL
LEFT JOIN REPORTING_WEB_UI_UAT.GENERATING_STATION_TYPE gst
    ON gst.ID = gs.STATION_TYPE
WHERE scd.SCADA_FILE_NAME_ID = 7
ORDER BY gs.GENERATING_STATION_NAME";

            using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();

            using var command = new OracleCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var model = new ScadaCellMappingModel
                {
                    Id = reader["ID"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(reader["ID"]),

                    GeneratingStationName = reader["GENERATING_STATION_NAME"] == DBNull.Value
                        ? ""
                        : reader["GENERATING_STATION_NAME"].ToString(),

                    StationTypeName = reader["STATION_TYPE_NAME"] == DBNull.Value
                        ? ""
                        : reader["STATION_TYPE_NAME"].ToString(),

                    Fuel = reader["FUEL"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["FUEL"]),

                    FuelType = reader["FUEL_TYPE"] == DBNull.Value
                        ? ""
                        : reader["FUEL_TYPE"].ToString(),

                    CellReference = reader["CELL_REFERENCE"] == DBNull.Value
                        ? ""
                        : reader["CELL_REFERENCE"].ToString()
                };

                result.Add(model);
            }

            return result;
        }
        // Temport export feature

        public async Task<IActionResult> OnGetRunExportAsync()
        {
            await _exportService.GenerateAndUploadFilesAsync();

            return Content("Export Completed");
        }

        private static void StyleLabelCell(ExcelRange cell)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 30, 60));
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        private async Task<(bool IsAdmin, bool IsSldc, string State)> GetCurrentUserInfo()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return (false, false, null);

            if (user.UserName.Equals("admin",
                StringComparison.OrdinalIgnoreCase))
            {
                return (true, false, null);
            }

            var userDetails = await _context.UserDetails
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (userDetails?.IsSldc == true)
            {
                return (false, true, userDetails.State);
            }

            return (false, false, null);
        }
    }
}