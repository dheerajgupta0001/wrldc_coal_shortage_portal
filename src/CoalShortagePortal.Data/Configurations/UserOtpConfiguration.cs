using CoalShortagePortal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoalShortagePortal.Data.Configurations
{
    public class UserOtpConfiguration : IEntityTypeConfiguration<UserOtp>
    {
        public void Configure(EntityTypeBuilder<UserOtp> builder)
        {
            builder
                .HasIndex(u => new { u.UserId, u.OtpCode, u.IsUsed });
        }
    }
}