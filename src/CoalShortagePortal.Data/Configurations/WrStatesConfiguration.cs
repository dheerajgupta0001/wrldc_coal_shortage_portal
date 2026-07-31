using CoalShortagePortal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoalShortagePortal.Data.Configurations
{
    public class WrStatesConfiguration : IEntityTypeConfiguration<WrStates>
    {
        public void Configure(EntityTypeBuilder<WrStates> builder)
        {
            builder
                .HasIndex(w => new { w.Id });
        }
    }
}