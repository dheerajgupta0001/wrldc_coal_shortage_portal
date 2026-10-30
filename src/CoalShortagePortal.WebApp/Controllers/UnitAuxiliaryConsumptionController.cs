using CoalShortagePortal.Application.Features.GenMUs.Interfaces;
using CoalShortagePortal.Core;
using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Data;
using CoalShortagePortal.WebApp.Extensions;
using CoalShortagePortal.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Controllers
{
    [Authorize(Roles = SecurityConstants.AdminRoleString)]
    public class UnitAuxiliaryConsumptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportingOracleService _reportingOracleService;
        private readonly UserManager<IdentityUser> _userManager;

        public UnitAuxiliaryConsumptionController(
            ApplicationDbContext context,
            IReportingOracleService reportingOracleService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _reportingOracleService = reportingOracleService;
            _userManager = userManager;
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
                .ToListAsync();

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
                UpdatedOn = x.Created
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UnitAuxiliaryConsumptionCreateVM();
            await PopulateStationsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UnitAuxiliaryConsumptionCreateVM model)
        {
            var stations = await _reportingOracleService.GetGeneratingStationsAsync();
            if (!model.GeneratingStationId.HasValue
                || !stations.Any(x => x.Id == model.GeneratingStationId.Value))
            {
                ModelState.AddModelError(nameof(model.GeneratingStationId), "Select a valid generating station.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateStationsAsync(model, stations);
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var now = DateTime.UtcNow;
            _context.UnitAuxiliaryConsumptions.Add(new UnitAuxiliaryConsumption
            {
                GeneratingStationId = model.GeneratingStationId.Value,
                UnitNumber = model.UnitNumber,
                InstalledCapacityMw = model.InstalledCapacityMw,
                AuxiliaryConsumptionPercent = model.AuxiliaryConsumptionPercent,
                CreatedById = currentUser?.Id ?? "system",
                Created = now,
                LastModifiedById = currentUser?.Id ?? "system",
                LastModified = now
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)).WithSuccess("Unit auxiliary-consumption data saved.");
        }

        private async Task PopulateStationsAsync(
            UnitAuxiliaryConsumptionCreateVM model,
            IReadOnlyCollection<CoalShortagePortal.Application.Features.GenMUs.Models.GeneratingStationOptionModel> stations = null)
        {
            stations ??= await _reportingOracleService.GetGeneratingStationsAsync();
            model.GeneratingStations = stations
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name,
                    Selected = x.Id == model.GeneratingStationId
                })
                .ToList();
        }
    }
}
