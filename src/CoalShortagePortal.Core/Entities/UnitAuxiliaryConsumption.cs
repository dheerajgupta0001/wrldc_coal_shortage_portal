using System.ComponentModel.DataAnnotations.Schema;
using CoalShortagePortal.Core.Interfaces;

namespace CoalShortagePortal.Core.Entities
{
    public class UnitAuxiliaryConsumption : AuditableEntity, IAggregateRoot
    {
        public int GeneratingStationId { get; set; }
        public int UnitNumber { get; set; }
        public decimal InstalledCapacityMw { get; set; }
        public decimal AuxiliaryConsumptionPercent { get; set; }

        [NotMapped]
        public decimal NormativeCapacityMw =>
            InstalledCapacityMw * (1m - AuxiliaryConsumptionPercent / 100m);
    }
}
