using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<IncidentReport> IncidentReports => Set<IncidentReport>();
    }
}