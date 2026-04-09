using CoalShortagePortal.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CoalShortagePortal.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        public DbSet<GeneratingStationForCoalShortage> GeneratingStationForCoalShortages { get; set; }
        public DbSet<GeneratingStationForCriticalCoal> GeneratingStationForCriticalCoals { get; set; }
        public DbSet<GeneratingStationForOtherReason> GeneratingStationForOtherReasons { get; set; }
        public DbSet<CoalShortageResponse> CoalShortageResponses { get; set; }
        public DbSet<OtherReasonsResponse> OtherReasonsResponses { get; set; }
        public DbSet<CriticalCoalResponse> CriticalCoalResponses { get; set; }
        public DbSet<ExpectedRevivalResponse> ExpectedRevivalResponses { get; set; }
        public DbSet<DailyMUsData> DailyMUsDatas { get; set; }
        public DbSet<GenStnStg> GenStnStgs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            // https://stackoverflow.com/questions/56799520/aspnetcore-2-1-identitydbcontext-how-to-get-current-username
            var httpContextAccessor = this.GetService<IHttpContextAccessor>();
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedById = userId;
                        entry.Entity.Created = DateTime.Now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedById = userId;
                        entry.Entity.LastModified = DateTime.Now;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
