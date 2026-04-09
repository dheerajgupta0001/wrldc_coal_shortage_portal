using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoalShortagePortal.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoalShortagePortal.Data.Configurations
{
    public class GenStnStgConfiguration : IEntityTypeConfiguration<GenStnStg>
    {
        public void Configure(EntityTypeBuilder<GenStnStg> builder)
        {

            builder
            .HasIndex(b => new { b.Stage, b.StationName})
            .IsUnique();
        }
    }
}
