using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Users;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
