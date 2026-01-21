using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoalShortagePortal.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoalShortagePortal.Data.Configurations
{
    public class DailyMUsDataConfiguration : IEntityTypeConfiguration<DailyMUsData>
    {
        public void Configure(EntityTypeBuilder<DailyMUsData> builder)
        {
            builder
            .HasIndex(b => new { b.DataDate, b.StationName})
            .IsUnique();
        }
    }
}
