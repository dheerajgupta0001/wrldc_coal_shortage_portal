using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.WebApp.Models.GenMUs;

namespace CoalShortagePortal.WebApp.Services.GenMUs.Interfaces
{
    public interface IAllPendingMUsService
    {
        Task<List<PendingMUViewModel>> GetPendingGeneratorsAsync(
            DateTime date,
            CurrentUserInfo currentUser);

        Task<List<MUEntryModel>> BuildMUEntriesAsync(
            List<PendingMUViewModel> pendingGenerators,
            List<string> editingStations);

        Task<SaveMUResult> SaveMUEntriesAsync(
            List<MUEntryModel> muEntries,
            DateTime date,
            CurrentUserInfo currentUser);

        Task<bool> DeleteMUEntryAsync(
            string stationName,
            DateTime date,
            CurrentUserInfo currentUser);

        Task<List<DailyMUsData>> GetYesterdaySubmissionsAsync(
            DateTime date);
    }
}