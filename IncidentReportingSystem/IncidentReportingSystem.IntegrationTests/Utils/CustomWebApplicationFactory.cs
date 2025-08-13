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

            // Apply test overrides last
            var testOverrides = new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "http://example.com",
                ["EnableSwagger"] = "false"
            };
            cfg.AddInMemoryCollection(testOverrides);
        });

        builder.ConfigureServices((context, services) =>
        {
            // ✅ Ensure CORS is registered for tests
            var origins = (context.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            services.AddCors(options =>
            {
                options.AddPolicy("Default", b =>
                {
                    if (origins.Length > 0)
                    {
                        b.WithOrigins(origins)
                         .AllowAnyHeader()
                         .AllowAnyMethod()
                         .AllowCredentials();
                    }
                    else
                    {
                        b.AllowAnyOrigin()
                         .AllowAnyHeader()
                         .AllowAnyMethod();
                    }
                });
            });

            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add DbContext with test connection string
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Run migrations for test DB
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }
}
