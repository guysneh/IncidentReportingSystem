using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
