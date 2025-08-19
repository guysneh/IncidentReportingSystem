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
            // Remove any prior EF registrations for ApplicationDbContext (pooled or not)
            var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>) ||
                    (d.ImplementationType?.Name?.Contains("PooledDbContextFactory", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.ServiceType.FullName?.Contains("PooledDbContextFactory", StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // Read the one canonical connection string (from Test config/env)
            using var sp0 = services.BuildServiceProvider();
            var cfg = sp0.GetRequiredService<IConfiguration>();
            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in Test.");

            // Re-add a single DbContext registration (mirror your Program.cs: AddDbContext or AddDbContextPool)
            services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(cs));
            // If your app uses pooling in Program.cs, prefer:
            // services.AddDbContextPool<ApplicationDbContext>(o => o.UseNpgsql(cs));

            // Apply migrations once so the schema is ready
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }
}
