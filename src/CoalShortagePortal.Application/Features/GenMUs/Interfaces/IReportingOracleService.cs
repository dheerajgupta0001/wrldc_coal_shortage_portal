using System.Collections.Generic;
using System.Threading.Tasks;
using CoalShortagePortal.Application.Features.GenMUs.Models;

namespace CoalShortagePortal.Application.Features.GenMUs.Interfaces
{
    public interface IReportingOracleService
    {
        Task<List<GeneratingStationOptionModel>> GetGeneratingStationsAsync();

        Task<List<ScadaCellMappingModel>> GetScadaCellMappingsAsync();

        Task<List<StateOwnedStationModel>> GetStateOwnedStationsAsync(
            string state);
    }
}