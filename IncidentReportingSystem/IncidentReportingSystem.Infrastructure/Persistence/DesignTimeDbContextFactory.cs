using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IncidentReportingSystem.Infrastructure.Persistence
{
    /// <summary>Ensures EF CLI uses the same config as the API at design-time.</summary>
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                      ?? "Development";

            // Point to the API project's folder
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "IncidentReportingSystem.API");

            var cfg = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

            var b = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(cs);

            return new ApplicationDbContext(b.Options);
        }
    }
}
