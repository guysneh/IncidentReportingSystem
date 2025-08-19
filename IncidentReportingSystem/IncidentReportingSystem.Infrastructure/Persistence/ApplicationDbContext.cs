using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Users;
using IncidentReportingSystem.Infrastructure.Persistence.Idempotency;
using IncidentReportingSystem.Infrastructure.Persistence.Configurations;

namespace IncidentReportingSystem.Infrastructure.Persistence
{
    /// <summary>
    /// Main EF Core DbContext for Incident Reporting System.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        /// <summary>
        /// Table for incident reports.
        /// </summary>
        public DbSet<IncidentReport> IncidentReports => Set<IncidentReport>();

        /// <summary>
        /// Table for application users (multi-role).
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Table for incident comments.
        /// </summary>
        public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();
        public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            modelBuilder.ApplyConfiguration(new IdempotencyRecordConfiguration());       
        }
    }
}
