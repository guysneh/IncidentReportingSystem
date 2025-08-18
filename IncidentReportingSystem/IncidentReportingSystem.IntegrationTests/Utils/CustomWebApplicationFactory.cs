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

        builder.ConfigureAppConfiguration((context, cfg) =>
        {
            cfg.Sources.Clear();

            cfg.SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true)
               .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables();

            // Test overrides (last wins)
            var testOverrides = new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "http://example.com",
                ["AppConfig:Enabled"] = "false" // do not wire Azure App Configuration in tests
            };
            cfg.AddInMemoryCollection(testOverrides);
        });

        builder.ConfigureServices((context, services) =>
        {
            // Ensure CORS policy exists in tests (as in the app)
            var origins = (context.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            services.AddCors(options =>
            {
                options.AddPolicy("Default", b =>
                {
                    if (origins.Length > 0)
                    {
                        b.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                    }
                    else
                    {
                        b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    }
                });
            });

            // Re-register DbContext with test connection string
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                                    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection for tests.");

            // Make this available to tests that do manual cleanup via Npgsql
            Environment.SetEnvironmentVariable("TEST_DB_CONNECTION", connectionString);

            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

            // Apply migrations
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }
}
