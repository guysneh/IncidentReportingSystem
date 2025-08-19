using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using IncidentReportingSystem.Infrastructure.Persistence;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, cfg) =>
        {
            var baseDir = AppContext.BaseDirectory;

            cfg.Sources.Clear();
            cfg.SetBasePath(baseDir)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables(); 
        });

        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.SetMinimumLevel(LogLevel.Information);
        });

        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            using var sp0 = services.BuildServiceProvider();
            var cfg = sp0.GetRequiredService<IConfiguration>();
            var connectionString =
                cfg.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in test environment.");

            Environment.SetEnvironmentVariable("TEST_DB_CONNECTION", connectionString);

            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseNpgsql(connectionString));

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }
}
