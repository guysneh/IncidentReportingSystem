using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IncidentReportingSystem.Infrastructure
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");

            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("ConnectionStrings__DefaultConnection (or _Default) is not set.");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(conn)
                .Options;

            return (ApplicationDbContext)Activator.CreateInstance(typeof(ApplicationDbContext), options)!;
        }
    }
}
