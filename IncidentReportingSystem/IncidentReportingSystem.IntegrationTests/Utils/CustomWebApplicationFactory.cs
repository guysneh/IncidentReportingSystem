using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IncidentReportingSystem.Infrastructure.Persistence;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.Sources.Clear();
            cfg.SetBasePath(AppContext.BaseDirectory)                  // <- output folder (bin/…)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true) // fail if missing
               .AddEnvironmentVariables();
        });

        builder.ConfigureServices(services =>
        {
            // Remove original registrations
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(ApplicationDbContext)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            // Read the *same* connection string the API will use
            using var sp0 = services.BuildServiceProvider();
            var cfg = sp0.GetRequiredService<IConfiguration>();
            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in Test.");

            // Re-register a single DbContext that both API and tests will use
            services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(cs));

            // Apply migrations now
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });

    }
}
