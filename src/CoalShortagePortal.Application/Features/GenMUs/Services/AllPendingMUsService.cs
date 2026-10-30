using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoalShortagePortal.Application.Features.GenMUs.Interfaces;
using CoalShortagePortal.Application.Features.GenMUs.Models;
using CoalShortagePortal.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CoalShortagePortal.Data;
using CoalShortagePortal.Application.Features.GenMUs.Helpers;

namespace CoalShortagePortal.Application.Features.GenMUs.Services
{
    public class AllPendingMUsService : IAllPendingMUsService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IReportingOracleService _reportingOracleService;
        private readonly ILogger<AllPendingMUsService> _logger;

        public AllPendingMUsService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IReportingOracleService reportingOracleService,
            ILogger<AllPendingMUsService> logger)
        {
            _context = context;
            _userManager = userManager;
            _reportingOracleService = reportingOracleService;
            _logger = logger;
        }

        public async Task<List<PendingMUViewModel>> GetPendingGeneratorsAsync(
            DateTime date,
            CurrentUserInfo currentUser)
        {
            var yesterdaySubmissions = await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == date
                         && !d.StationName.StartsWith("wr_"))
                .ToListAsync();

            var pendingGenerators =
                new List<PendingMUViewModel>();

            if (currentUser.IsAdmin)
            {
                var allUsers = await _userManager.Users
                    .Where(u => !u.UserName.StartsWith("wr_"))
                    .ToListAsync();

                var allUserIds = allUsers
                    .Select(u => u.Id)
                    .ToList();

                var userDetailsMap = await _context.UserDetails
                    .Where(ud => allUserIds.Contains(ud.UserId))
                    .ToDictionaryAsync(
                        ud => ud.UserId,
                        ud => ud.UserType);

                foreach (var user in allUsers)
                {
                    if (user.UserName != null &&
                        user.UserName.ToLower() == "admin")
                    {
                        continue;
                    }

                    var submission = yesterdaySubmissions
                        .FirstOrDefault(s =>
                            s.StationName == user.UserName);

                    string userType;
                    userDetailsMap.TryGetValue(
                        user.Id,
                        out userType);

                    pendingGenerators.Add(
                        new PendingMUViewModel
                        {
                            StationName = user.UserName,
                            Email = user.Email,
                            HasSubmitted = submission != null,
                            UserType = userType ?? "",

                            ExistingMUs = submission == null
                                ? null
                                : submission.DailyMUs,

                            ExistingScheduleMUs = submission == null
                                ? null
                                : submission.ScheduleMUs,

                            ExistingExBus = submission == null
                                ? null
                                : submission.ExBus,

                            ExistingPeakMW = submission == null
                                ? null
                                : submission.PeakMW,

                            ExistingPeakMWTime = submission == null
                                ? null
                                : submission.PeakMWTime,

                            ExistingOffPeakMW = submission == null
                                ? null
                                : submission.OffPeakMW,

                            ExistingOffPeakMWTime = submission == null
                                ? null
                                : submission.OffPeakMWTime,

                            ExistingDayPeakMW = submission == null
                                ? null
                                : submission.DayPeakMW,

                            ExistingDayPeakMWTime = submission == null
                                ? null
                                : submission.DayPeakMWTime,

                            ExistingMinGeneration = submission == null
                                ? null
                                : submission.MinGeneration,

                            ExistingMinGenerationTime = submission == null
                                ? null
                                : submission.MinGenerationTime
                        });
                }
            }
            else if (currentUser.IsSldc)
            {
                var stateOwnedStations =
                    await _reportingOracleService
                        .GetStateOwnedStationsAsync(currentUser.State);

                foreach (var station in stateOwnedStations)
                {
                    var submission = yesterdaySubmissions
                        .FirstOrDefault(s =>
                            StationNameHelper.NormalizeStationName(
                                s.StationName)
                            ==
                            StationNameHelper.NormalizeStationName(
                                station.StationName));

                    pendingGenerators.Add(
                        new PendingMUViewModel
                        {
                            StationName = station.StationName,
                            Email = "",
                            HasSubmitted = submission != null,
                            UserType = "STATE_GEN",

                            ExistingMUs = submission == null
                                ? null
                                : submission.DailyMUs,

                            ExistingScheduleMUs = submission == null
                                ? null
                                : submission.ScheduleMUs,

                            ExistingExBus = submission == null
                                ? null
                                : submission.ExBus,

                            ExistingPeakMW = submission == null
                                ? null
                                : submission.PeakMW,

                            ExistingPeakMWTime = submission == null
                                ? null
                                : submission.PeakMWTime,

                            ExistingOffPeakMW = submission == null
                                ? null
                                : submission.OffPeakMW,

                            ExistingOffPeakMWTime = submission == null
                                ? null
                                : submission.OffPeakMWTime,

                            ExistingDayPeakMW = submission == null
                                ? null
                                : submission.DayPeakMW,

                            ExistingDayPeakMWTime = submission == null
                                ? null
                                : submission.DayPeakMWTime,

                            ExistingMinGeneration = submission == null
                                ? null
                                : submission.MinGeneration,

                            ExistingMinGenerationTime = submission == null
                                ? null
                                : submission.MinGenerationTime
                        });
                }
            }

            return pendingGenerators
                .OrderBy(p => p.HasSubmitted)
                .ThenBy(p => p.StationName)
                .ToList();
        }

        public Task<List<MUEntryModel>> BuildMUEntriesAsync(
            List<PendingMUViewModel> pendingGenerators,
            List<string> editingStations)
        {
            var muEntries = new List<MUEntryModel>();

            foreach (var p in pendingGenerators)
            {
                bool isEditing =
                    editingStations != null &&
                    editingStations.Contains(p.StationName);

                if (!p.HasSubmitted)
                {
                    muEntries.Add(
                        new MUEntryModel
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
                    muEntries.Add(
                        new MUEntryModel
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
                            MinGenerationTime =
                                p.ExistingMinGenerationTime
                        });
                }
            }

            return Task.FromResult(muEntries);
        }

        public async Task<SaveMUResult> SaveMUEntriesAsync(
            List<MUEntryModel> muEntries,
            DateTime date,
            CurrentUserInfo currentUser)
        {
            var result = new SaveMUResult();

            var entriesToProcess = muEntries == null
                ? new List<MUEntryModel>()
                : muEntries
                    .Where(e => e.DailyMUs.HasValue &&
                                e.DailyMUs.Value > 0)
                    .ToList();

            if (currentUser.IsSldc)
            {
                var allowedStations =
                    await _reportingOracleService
                        .GetStateOwnedStationsAsync(currentUser.State);

                var allowedStationKeys = allowedStations
                    .Select(x =>
                        StationNameHelper.NormalizeStationName(
                            x.StationName))
                    .ToHashSet();

                entriesToProcess = entriesToProcess
                    .Where(x =>
                        allowedStationKeys.Contains(
                            StationNameHelper.NormalizeStationName(
                                x.StationName)))
                    .ToList();
            }

            if (!entriesToProcess.Any())
            {
                result.ErrorMessages.Add(
                    "No MU values entered. Please enter at least one value to save.");

                return result;
            }

            foreach (var entry in entriesToProcess)
            {
                try
                {
                    string dbUserType = entry.UserType ?? "";

                    if (currentUser.IsAdmin)
                    {
                        var stationUser =
                            await _userManager.FindByNameAsync(
                                entry.StationName);

                        if (stationUser != null)
                        {
                            dbUserType = await _context.UserDetails
                                .Where(ud => ud.UserId == stationUser.Id)
                                .Select(ud => ud.UserType)
                                .FirstOrDefaultAsync() ?? "";
                        }
                    }

                    if (currentUser.IsSldc)
                    {
                        dbUserType = "STATE_GEN";
                    }

                    bool isScheduleUser =
                        dbUserType == "ISGS" ||
                        dbUserType == "IPP";

                    if (!entry.DailyMUs.HasValue ||
                        entry.DailyMUs.Value <= 0)
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": Gross MUs is required.");

                        continue;
                    }

                    if (!entry.ExBus.HasValue ||
                        entry.ExBus.Value <= 0)
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": ExBus must be greater than 0.");

                        continue;
                    }

                    if (!entry.PeakMW.HasValue)
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": Peak MW is required.");

                        continue;
                    }

                    if (!entry.OffPeakMW.HasValue)
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": Off Peak MW is required.");

                        continue;
                    }

                    if (!entry.DayPeakMW.HasValue)
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": Day Peak MW is required.");

                        continue;
                    }

                    if (!entry.DayPeakMWTime.HasValue)
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": Day Peak MW Time is required.");

                        continue;
                    }

                    if (!entry.MinGeneration.HasValue)
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": Min Generation is required.");

                        continue;
                    }

                    if (!entry.MinGenerationTime.HasValue)
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": Min Generation Time is required.");

                        continue;
                    }

                    if (isScheduleUser &&
                        (!entry.ScheduleMUs.HasValue ||
                         entry.ScheduleMUs.Value <= 0))
                    {
                        result.ErrorMessages.Add(
                            entry.StationName +
                            ": Schedule MUs required for " +
                            dbUserType + " stations.");

                        continue;
                    }

                    var existing = await _context.DailyMUsDatas
                        .FirstOrDefaultAsync(d =>
                            d.DataDate.Date == date
                            && d.StationName == entry.StationName);

                    if (existing != null)
                    {
                        existing.DailyMUs = entry.DailyMUs.Value;

                        existing.ScheduleMUs = isScheduleUser
                            ? entry.ScheduleMUs ?? 0f
                            : 0f;

                        existing.ExBus = entry.ExBus.Value;

                        existing.PeakMW = entry.PeakMW.Value;
                        existing.PeakMWTime =
                            entry.PeakMWTime ??
                            new TimeSpan(19, 0, 0);

                        existing.OffPeakMW =
                            entry.OffPeakMW.Value;
                        existing.OffPeakMWTime =
                            entry.OffPeakMWTime ??
                            new TimeSpan(3, 0, 0);

                        existing.DayPeakMW =
                            entry.DayPeakMW.Value;
                        existing.DayPeakMWTime =
                            entry.DayPeakMWTime.Value;

                        existing.MinGeneration =
                            entry.MinGeneration.Value;
                        existing.MinGenerationTime =
                            entry.MinGenerationTime.Value;

                        existing.LastModified = DateTime.UtcNow;

                        result.EditCount++;
                    }
                    else
                    {
                        var newData = new DailyMUsData
                        {
                            DataDate = DateTime.SpecifyKind(
                                date,
                                DateTimeKind.Utc),

                            DailyMUs = entry.DailyMUs.Value,

                            ScheduleMUs = isScheduleUser
                                ? entry.ScheduleMUs ?? 0f
                                : 0f,

                            ExBus = entry.ExBus.Value,

                            PeakMW = entry.PeakMW.Value,
                            PeakMWTime =
                                entry.PeakMWTime ??
                                new TimeSpan(19, 0, 0),

                            OffPeakMW = entry.OffPeakMW.Value,
                            OffPeakMWTime =
                                entry.OffPeakMWTime ??
                                new TimeSpan(3, 0, 0),

                            DayPeakMW = entry.DayPeakMW.Value,
                            DayPeakMWTime =
                                entry.DayPeakMWTime.Value,

                            MinGeneration =
                                entry.MinGeneration.Value,
                            MinGenerationTime =
                                entry.MinGenerationTime.Value,

                            StationName = entry.StationName
                        };

                        _context.DailyMUsDatas.Add(newData);

                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing MU entry for station {Station}",
                        entry.StationName);

                    result.ErrorMessages.Add(
                        entry.StationName +
                        ": Failed to process. Please try again.");
                }
            }

            if (result.SuccessCount > 0 || result.EditCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return result;
        }

        public async Task<bool> DeleteMUEntryAsync(
            string stationName,
            DateTime date,
            CurrentUserInfo currentUser)
        {
            if (string.IsNullOrWhiteSpace(stationName))
                return false;

            if (stationName.StartsWith("wr_"))
                return false;

            if (currentUser.IsAdmin)
            {
                var stationUser =
                    await _userManager.FindByNameAsync(stationName);

                if (stationUser == null)
                    return false;
            }

            if (currentUser.IsSldc)
            {
                var allowedStations =
                    await _reportingOracleService
                        .GetStateOwnedStationsAsync(currentUser.State);

                var allowedStationKeys = allowedStations
                    .Select(x =>
                        StationNameHelper.NormalizeStationName(
                            x.StationName))
                    .ToHashSet();

                if (!allowedStationKeys.Contains(
                    StationNameHelper.NormalizeStationName(stationName)))
                {
                    return false;
                }
            }

            var record = await _context.DailyMUsDatas
                .FirstOrDefaultAsync(d =>
                    d.DataDate.Date == date
                    && d.StationName == stationName);

            if (record == null)
                return false;

            _context.DailyMUsDatas.Remove(record);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<DailyMUsData>> GetYesterdaySubmissionsAsync(
            DateTime date)
        {
            return await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == date
                         && !d.StationName.StartsWith("wr_"))
                .OrderBy(d => d.StationName)
                .ToListAsync();
        }
    }
}