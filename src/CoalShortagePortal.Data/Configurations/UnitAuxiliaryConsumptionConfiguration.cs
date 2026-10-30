using CoalShortagePortal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoalShortagePortal.Data.Configurations
{
    public class UnitAuxiliaryConsumptionConfiguration : IEntityTypeConfiguration<UnitAuxiliaryConsumption>
    {
        public void Configure(EntityTypeBuilder<UnitAuxiliaryConsumption> builder)
        {
            builder.Property(x => x.InstalledCapacityMw).HasPrecision(12, 3);
            builder.Property(x => x.AuxiliaryConsumptionPercent).HasPrecision(5, 2);

            builder.HasIndex(x => new
            {
                x.GeneratingStationId,
                x.UnitNumber,
                x.Created
            });
        }
    }
}
