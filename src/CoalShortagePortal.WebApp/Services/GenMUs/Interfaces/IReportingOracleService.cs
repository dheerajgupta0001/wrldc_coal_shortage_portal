using System.Collections.Generic;
using System.Threading.Tasks;
using CoalShortagePortal.WebApp.Models.GenMUs;

namespace CoalShortagePortal.WebApp.Services.GenMUs.Interfaces
{
    public interface IReportingOracleService
    {
        Task<List<ScadaCellMappingModel>> GetScadaCellMappingsAsync();

        Task<List<StateOwnedStationModel>> GetStateOwnedStationsAsync(
            string state);
    }
}