using System;

namespace CoalShortagePortal.WebApp.Models
{
    public class UnitAuxiliaryConsumptionListItemVM
    {
        public int GeneratingStationId { get; set; }
        public string StationName { get; set; }
        public int UnitNumber { get; set; }
        public decimal InstalledCapacityMw { get; set; }
        public decimal AuxiliaryConsumptionPercent { get; set; }
        public decimal NormativeCapacityMw { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool IsLatest { get; set; }
    }
}
