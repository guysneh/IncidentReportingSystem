using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string BasePath { get; private set; } = "/";
    public string ApiVersionSegment { get; private set; } = "v1";
    public IReadOnlyList<string> ApiVersions { get; private set; } = Array.Empty<string>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, cfg) =>
        {
            cfg.Sources.Clear();
            cfg.SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
               .AddEnvironmentVariables();
        });

        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
        });

        builder.ConfigureServices(services =>
        {
            // 1) Remove any EF registrations (pooled or not)
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>) ||
                    (d.ServiceType.FullName?.Contains("PooledDbContextFactory", StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // 2) Build a temporary provider to read configuration
            using var sp0 = services.BuildServiceProvider();
            var cfg = sp0.GetRequiredService<IConfiguration>();

            // 3) Single canonical connection string
            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in Test.");

            // 4) Register DbContext (mirror Program.cs style; if Program uses pooling, use AddDbContextPool)
            services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(cs));
            // or: services.AddDbContextPool<ApplicationDbContext>(o => o.UseNpgsql(cs));

            // 5) Apply migrations
            using (var sp = services.BuildServiceProvider())
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }

            // 6) Resolve BasePath
            BasePath = cfg["Api:BasePath"];
            BasePath = string.IsNullOrWhiteSpace(BasePath) || BasePath == "/" ? "/" : BasePath.TrimEnd('/');

            using (var sp1 = services.BuildServiceProvider())
            {
                var provider = sp1.GetService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
                if (provider != null)
                {
                    ApiVersionSegment = provider.ApiVersionDescriptions
                        .Where(d => !d.IsDeprecated)
                        .OrderByDescending(d => d.ApiVersion.MajorVersion ?? 0)
                        .ThenByDescending(d => d.ApiVersion.MinorVersion ?? 0)
                        .Select(d => d.GroupName)
                        .FirstOrDefault() ?? "v1";
                }
            }


            // 7) Resolve API versions from ApiExplorer (prefer non-deprecated highest)
            using (var sp1 = services.BuildServiceProvider())
            {
                var provider = sp1.GetService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
                if (provider != null)
                {
                    ApiVersions = provider.ApiVersionDescriptions
                                          .Where(d => !d.IsDeprecated)
                                          .Select(d => d.GroupName!)
                                          .ToArray();

                    ApiVersionSegment = ApiVersions
                        .OrderByDescending(v => v) // simple string order "v2.0" > "v1.0"
                        .FirstOrDefault() ?? "v1";
                }
            }
        });
    }
}
