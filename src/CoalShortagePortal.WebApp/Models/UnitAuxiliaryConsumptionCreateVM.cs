using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoalShortagePortal.WebApp.Models
{
    public class UnitAuxiliaryConsumptionCreateVM
    {
        [Required]
        [Display(Name = "Generating Station")]
        public int? GeneratingStationId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "Unit No")]
        public int UnitNumber { get; set; }

        [Required]
        [Range(typeof(decimal), "0.001", "999999999")]
        [Display(Name = "Installed Capacity (MW)")]
        public decimal InstalledCapacityMw { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "100")]
        [Display(Name = "Auxiliary Consumption (%)")]
        public decimal AuxiliaryConsumptionPercent { get; set; }

        public List<SelectListItem> GeneratingStations { get; set; } = new();
    }
}
