using CoalShortagePortal.Application.Features.GenMUs.Interfaces;
using CoalShortagePortal.Data;
using CoalShortagePortal.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Controllers
{
    [Authorize]
    public class UnitDataController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportingOracleService _reportingOracleService;

        public UnitDataController(
            ApplicationDbContext context,
            IReportingOracleService reportingOracleService)
        {
            _context = context;
            _reportingOracleService = reportingOracleService;
        }

        public async Task<IActionResult> Index()
        {
            var stations = await _reportingOracleService.GetGeneratingStationsAsync();
            var stationNames = stations.ToDictionary(x => x.Id, x => x.Name);
            var records = await _context.UnitAuxiliaryConsumptions
                .AsNoTracking()
                .OrderBy(x => x.GeneratingStationId)
                .ThenBy(x => x.UnitNumber)
                .ThenByDescending(x => x.Created)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var latestIds = records
                .GroupBy(x => new { x.GeneratingStationId, x.UnitNumber })
                .Select(x => x.First().Id)
                .ToHashSet();

            var model = records.Select(x => new UnitAuxiliaryConsumptionListItemVM
            {
                GeneratingStationId = x.GeneratingStationId,
                StationName = stationNames.TryGetValue(x.GeneratingStationId, out var name)
                    ? name
                    : $"Station {x.GeneratingStationId}",
                UnitNumber = x.UnitNumber,
                InstalledCapacityMw = x.InstalledCapacityMw,
                AuxiliaryConsumptionPercent = x.AuxiliaryConsumptionPercent,
                NormativeCapacityMw = x.NormativeCapacityMw,
                UpdatedOn = x.Created,
                IsLatest = latestIds.Contains(x.Id)
            }).ToList();

            return View(model);
        }
    }
}
