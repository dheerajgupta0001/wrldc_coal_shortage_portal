using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
        }

        public class MUEntryModel
        {
            public string StationName { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "Gross must be greater than 0")]
            public float? DailyMUs { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "Schedule must be greater than 0")]
            public float? ScheduleMUs { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "ExBus must be greater than 0")]
            public float? ExBus { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Peak MW must be greater than 0")]
            public int? PeakMW { get; set; }

            [DataType(DataType.Time)]
            public TimeSpan? PeakMWTime { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Off Peak MW must be greater than 0")]
            public int? OffPeakMW { get; set; }

            [DataType(DataType.Time)]
            public TimeSpan? OffPeakMWTime { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Day Peak MW must be greater than 0")]
            public int? DayPeakMW { get; set; }

            [DataType(DataType.Time)]
            public TimeSpan? DayPeakMWTime { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Min Generation must be greater than 0")]
            public int? MinGeneration { get; set; }

            [DataType(DataType.Time)]
            public TimeSpan? MinGenerationTime { get; set; }
        }

        // ??? EXISTING: GET ????????????????????????????????????????????
        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. This page is only accessible to administrators.";
                return RedirectToPage("/GenMUs/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var allUsers = await _userManager.Users
                .Where(u => !u.UserName.StartsWith("wr_"))
                .ToListAsync();

            var yesterdaySubmissions = await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == YesterdayDate && !d.StationName.StartsWith("wr_"))
                .ToListAsync();

            PendingGenerators = new List<PendingMUViewModel>();

            foreach (var user in allUsers)
            {
                if (user.UserName?.ToLower() == "admin")
                    continue;

                var submission = yesterdaySubmissions.FirstOrDefault(s => s.StationName == user.UserName);

                PendingGenerators.Add(new PendingMUViewModel
                {
                    StationName = user.UserName,
                    Email = user.Email,
                    HasSubmitted = submission != null,
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

            MUEntries = PendingGenerators
                .Where(p => !p.HasSubmitted)
                .Select(p => new MUEntryModel { 
                    StationName = p.StationName, 
                    // Set the requested default times here
                    PeakMWTime = new TimeSpan(19, 0, 0),    // Default to 19:00
                    OffPeakMWTime = new TimeSpan(3, 0, 0)
                })
                .ToList();

            return Page();
        }

        // ??? EXISTING: POST (Save All) ????????????????????????????????
        public async Task<IActionResult> OnPostAsync()
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. This page is only accessible to administrators.";
                return RedirectToPage("/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var successCount = 0;
            var errorMessages = new List<string>();

            var entriesToSave = MUEntries?.Where(e => e.DailyMUs.HasValue && e.DailyMUs > 0).ToList()
                                ?? new List<MUEntryModel>();

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
                    if (entry.ExBus.HasValue && entry.ExBus <= 0)
                    {
                        errorMessages.Add($"{entry.StationName}: ExBus must be greater than 0");
                        continue;
                    }

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
                        ScheduleMUs = entry.ScheduleMUs.Value,
                        ExBus = entry.ExBus.Value,
                        PeakMW = entry.PeakMW.Value,

                        PeakMWTime = entry.PeakMWTime ?? new TimeSpan(19, 0, 0),
                        OffPeakMW = entry.OffPeakMW.Value,

                        OffPeakMWTime = entry.OffPeakMWTime ?? new TimeSpan(3, 0, 0),
                        DayPeakMW = entry.DayPeakMW.Value,
                        DayPeakMWTime = entry.DayPeakMWTime.Value,
                        MinGeneration = entry.MinGeneration.Value,
                        MinGenerationTime = entry.MinGenerationTime.Value,
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
                    TempData["SuccessMessage"] = $"Successfully saved {successCount} MU & ExBus record(s)";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error saving to database: {ex.Message}";
                    await OnGetAsync();
                    return Page();
                }
            }

            if (errorMessages.Any())
                TempData["ErrorMessage"] = string.Join("<br/>", errorMessages);

            return RedirectToPage();
        }

        // ??? EXISTING: POST Delete ????????????????????????????????????
        public async Task<IActionResult> OnPostDeleteAsync(string stationName)
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. This page is only accessible to administrators.";
                return RedirectToPage("/Index");
            }

            if (stationName.StartsWith("wr_"))
            {
                TempData["ErrorMessage"] = "Cannot delete records for stations starting with 'wr_'";
                return RedirectToPage();
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var record = await _context.DailyMUsDatas
                .FirstOrDefaultAsync(d => d.DataDate.Date == YesterdayDate
                                       && d.StationName == stationName);

            if (record != null)
            {
                _context.DailyMUsDatas.Remove(record);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Record for Generating Station {stationName} deleted successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Record not found";
            }

            return RedirectToPage();
        }

        // ??? NEW: GET ExportExcel ?????????????????????????????????????
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            if (User.Identity.Name?.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage();
            }

            var yesterday = DateTime.Now.AddDays(-1).Date;

            // Safety check — block export if anyone still pending
            var allUsers = await _userManager.Users
                .Where(u => !u.UserName.StartsWith("wr_") && u.UserName.ToLower() != "admin")
                .ToListAsync();

            var submissions = await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == yesterday && !d.StationName.StartsWith("wr_"))
                .OrderBy(d => d.StationName)
                .ToListAsync();

            var pendingStations = allUsers
                .Where(u => !submissions.Any(s => s.StationName == u.UserName))
                .ToList();

            if (pendingStations.Any())
            {
                TempData["ErrorMessage"] = $"Cannot export — {pendingStations.Count} station(s) have not submitted yet.";
                return RedirectToPage();
            }

            // ?? Build Excel ??????????????????????????????????????????
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage.License.SetNonCommercialPersonal("CoalShortagePortal");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("MU Data");

            int totalCols = submissions.Count + 1; // +1 for label column A

            // ?? Row 1: Title banner (merged) ?????????????????????????
            ws.Cells[1, 1, 1, totalCols].Merge = true;
            ws.Cells[1, 1].Value = $"Daily MU & ExBus Data — {yesterday:dd-MMM-yyyy}";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 13;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(102, 126, 234));
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Row(1).Height = 26;

            // ?? Row 2: Station names ?????????????????????????????????
            ws.Cells[2, 1].Value = "Station";
            StyleLabelCell(ws.Cells[2, 1]);

            for (int i = 0; i < submissions.Count; i++)
            {
                var cell = ws.Cells[2, i + 2];
                cell.Value = submissions[i].StationName;
                cell.Style.Font.Bold = true;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 220, 245));
            }

            // ?? Row 3: Daily MUs ?????????????????????????????????????
            ws.Cells[3, 1].Value = "Daily MUs";
            StyleLabelCell(ws.Cells[3, 1]);

            for (int i = 0; i < submissions.Count; i++)
            {
                var cell = ws.Cells[3, i + 2];
                cell.Value = submissions[i].DailyMUs;
                cell.Style.Numberformat.Format = "#,##0.00";
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 253, 231));
            }

            // ?? Row 4: ExBus ?????????????????????????????????????????
            ws.Cells[4, 1].Value = "ExBus";
            StyleLabelCell(ws.Cells[4, 1]);

            for (int i = 0; i < submissions.Count; i++)
            {
                var cell = ws.Cells[4, i + 2];
                cell.Value = submissions[i].ExBus;
                cell.Style.Numberformat.Format = "#,##0.00";
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(232, 245, 233));
            }

            // ?? Borders on data block (rows 2-4) ?????????????????????
            var dataBlock = ws.Cells[2, 1, 4, totalCols];
            dataBlock.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataBlock.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataBlock.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataBlock.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataBlock.Style.Border.BorderAround(ExcelBorderStyle.Medium);

            // ?? Auto-fit columns ??????????????????????????????????????
            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            ws.Column(1).Width = 14;
            for (int c = 2; c <= totalCols; c++)
                ws.Column(c).Width = Math.Max(ws.Column(c).Width, 14);

            ws.Row(2).Height = 20;
            ws.Row(3).Height = 20;
            ws.Row(4).Height = 20;

            // ?? Return as download ????????????????????????????????????
            var bytes = package.GetAsByteArray();
            var fileName = $"MU_Data_{yesterday:yyyyMMdd}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ?? Helper: style the label cells in column A ?????????????????
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
