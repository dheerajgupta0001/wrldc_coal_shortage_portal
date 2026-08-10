using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CoalShortagePortal.Core.Interfaces.ReportingData;
using CoalShortagePortal.WebApp.Models.GenMUs;
using CoalShortagePortal.WebApp.Services.GenMUs.Helpers;
using CoalShortagePortal.WebApp.Services.GenMUs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace CoalShortagePortal.WebApp.Pages.GenMUs
{
    [Authorize]
    public class AllPendingMUsModel : PageModel
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IAllPendingMUsService _allPendingMUsService;
        private readonly IReportingOracleService _reportingOracleService;
        private readonly IExcelCellExportService _excelCellExportService;
        private readonly IExBusGenMUsExportService _exportService;
        private readonly ILogger<AllPendingMUsModel> _logger;

        public AllPendingMUsModel(
            ICurrentUserService currentUserService,
            IAllPendingMUsService allPendingMUsService,
            IReportingOracleService reportingOracleService,
            IExcelCellExportService excelCellExportService,
            IExBusGenMUsExportService exportService,
            ILogger<AllPendingMUsModel> logger)
        {
            _currentUserService = currentUserService;
            _allPendingMUsService = allPendingMUsService;
            _reportingOracleService = reportingOracleService;
            _excelCellExportService = excelCellExportService;
            _exportService = exportService;
            _logger = logger;
        }

        public List<PendingMUViewModel> PendingGenerators { get; set; }
            = new List<PendingMUViewModel>();

        public DateTime YesterdayDate { get; set; }

        public List<string> EditingStations { get; set; }
            = new List<string>();

        [BindProperty]
        public List<MUEntryModel> MUEntries { get; set; }
            = new List<MUEntryModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser =
                await _currentUserService
                    .GetCurrentUserInfoAsync(User);

            if (!currentUser.IsAdmin && !currentUser.IsSldc)
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage("/GenMUs/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            LoadEditingStationsFromTempData();

            await LoadPageDataAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetToggleEdit(
            string stationName)
        {
            var currentUser =
                await _currentUserService
                    .GetCurrentUserInfoAsync(User);

            if (!currentUser.IsAdmin && !currentUser.IsSldc)
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage("/GenMUs/Index");
            }

            if (string.IsNullOrWhiteSpace(stationName))
                return RedirectToPage("./AllPendingMUs");

            if (currentUser.IsSldc)
            {
                var allowedStations =
                    await _reportingOracleService
                        .GetStateOwnedStationsAsync(
                            currentUser.State);

                var allowedStationKeys = allowedStations
                    .Select(x =>
                        StationNameHelper.NormalizeStationName(
                            x.StationName))
                    .ToHashSet();

                if (!allowedStationKeys.Contains(
                    StationNameHelper.NormalizeStationName(
                        stationName)))
                {
                    TempData["ErrorMessage"] =
                        "You are not authorised to edit this station.";

                    return RedirectToPage("./AllPendingMUs");
                }
            }

            var editing = new List<string>();

            if (TempData["EditingStations"] is string existing &&
                !string.IsNullOrEmpty(existing))
            {
                editing =
                    JsonSerializer.Deserialize<List<string>>(existing);
            }

            if (!editing.Contains(stationName))
                editing.Add(stationName);

            TempData["EditingStations"] =
                JsonSerializer.Serialize(editing);

            return RedirectToPage("./AllPendingMUs");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUser =
                await _currentUserService
                    .GetCurrentUserInfoAsync(User);

            if (!currentUser.IsAdmin && !currentUser.IsSldc)
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage("/GenMUs/Index");
            }

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var result =
                await _allPendingMUsService.SaveMUEntriesAsync(
                    MUEntries,
                    YesterdayDate,
                    currentUser);

            if (result.HasSuccess)
            {
                TempData.Remove("EditingStations");

                var parts = new List<string>();

                if (result.SuccessCount > 0)
                {
                    parts.Add(
                        result.SuccessCount +
                        " new record(s) saved");
                }

                if (result.EditCount > 0)
                {
                    parts.Add(
                        result.EditCount +
                        " record(s) updated");
                }

                TempData["SuccessMessage"] =
                    string.Join(", ", parts) + " successfully!";
            }

            if (result.HasErrors)
            {
                TempData["ErrorMessage"] =
                    string.Join("<br/>", result.ErrorMessages);

                if (!result.HasSuccess)
                {
                    await LoadPageDataAsync();
                    return Page();
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            string stationName)
        {
            var currentUser =
                await _currentUserService
                    .GetCurrentUserInfoAsync(User);

            if (!currentUser.IsAdmin && !currentUser.IsSldc)
            {
                TempData["ErrorMessage"] = "Access denied.";
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

            YesterdayDate = DateTime.Now.AddDays(-1).Date;

            var deleted =
                await _allPendingMUsService.DeleteMUEntryAsync(
                    stationName,
                    YesterdayDate,
                    currentUser);

            if (deleted)
            {
                _logger.LogInformation(
                    "Record deleted for station {Station} by user {User}",
                    stationName,
                    User.Identity == null
                        ? ""
                        : User.Identity.Name);

                TempData["SuccessMessage"] =
                    "Record for station deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Record not found or not authorised.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetExportExBusExcelAsync()
        {
            var currentUser =
                await _currentUserService
                    .GetCurrentUserInfoAsync(User);

            if (!currentUser.IsAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized ExBus export attempt by {User}",
                    User.Identity == null
                        ? ""
                        : User.Identity.Name);

                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage();
            }

            var yesterday = DateTime.Now.AddDays(-1).Date;

            var submissions =
                await _allPendingMUsService
                    .GetYesterdaySubmissionsAsync(yesterday);

            var scadaMappings =
                await _reportingOracleService
                    .GetScadaCellMappingsAsync();

            ExcelPackage.License.SetNonCommercialPersonal(
                "CoalShortagePortal");

            var bytes =
                _excelCellExportService.GenerateCellReferenceExcel(
                    submissions,
                    scadaMappings,
                    "ExBus",
                    d => d.ExBus,
                    Color.FromArgb(232, 245, 233));

            string fileName =
                "ExBus_" + yesterday.ToString("dd_MM_yyyy") + ".xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        public async Task<IActionResult> OnGetExportGenMuExcelAsync()
        {
            var currentUser =
                await _currentUserService
                    .GetCurrentUserInfoAsync(User);

            if (!currentUser.IsAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized GenMU export attempt by {User}",
                    User.Identity == null
                        ? ""
                        : User.Identity.Name);

                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage();
            }

            var yesterday = DateTime.Now.AddDays(-1).Date;

            var submissions =
                await _allPendingMUsService
                    .GetYesterdaySubmissionsAsync(yesterday);

            var scadaMappings =
                await _reportingOracleService
                    .GetScadaCellMappingsAsync();

            ExcelPackage.License.SetNonCommercialPersonal(
                "CoalShortagePortal");

            var bytes =
                _excelCellExportService.GenerateCellReferenceExcel(
                    submissions,
                    scadaMappings,
                    "GenMU",
                    d => d.DailyMUs,
                    Color.FromArgb(255, 253, 231));

            string fileName =
                "GenMU_" + yesterday.ToString("dd_MM_yyyy") + ".xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            return await OnGetExportGenMuExcelAsync();
        }

        public async Task<IActionResult> OnGetRunExportAsync()
        {
            var currentUser =
                await _currentUserService
                    .GetCurrentUserInfoAsync(User);

            if (!currentUser.IsAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized export attempt by {User}",
                    User.Identity == null
                        ? ""
                        : User.Identity.Name);

                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToPage();
            }

            await _exportService.GenerateAndUploadFilesAsync();

            _logger.LogInformation(
                "Manual export triggered by {User}",
                User.Identity == null
                    ? ""
                    : User.Identity.Name);

            return Content("Export Completed");
        }

        private async Task LoadPageDataAsync()
        {
            var currentUser =
                await _currentUserService
                    .GetCurrentUserInfoAsync(User);

            PendingGenerators =
                await _allPendingMUsService
                    .GetPendingGeneratorsAsync(
                        YesterdayDate,
                        currentUser);

            MUEntries =
                await _allPendingMUsService
                    .BuildMUEntriesAsync(
                        PendingGenerators,
                        EditingStations);
        }

        private void LoadEditingStationsFromTempData()
        {
            if (TempData.Peek("EditingStations") is string editJson &&
                !string.IsNullOrEmpty(editJson))
            {
                EditingStations =
                    JsonSerializer.Deserialize<List<string>>(editJson);

                TempData.Keep("EditingStations");
            }
        }
    }
}