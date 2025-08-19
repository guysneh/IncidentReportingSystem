using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IncidentReportingSystem.Infrastructure.Persistence;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string ApiVersionSegment { get; private set; } = "v1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, cfg) =>
        {
            cfg.Sources.Clear();
            var baseDir = AppContext.BaseDirectory;

            cfg.SetBasePath(baseDir)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               // IMPORTANT: fail if the test config is missing (so CI won't silently misconfigure)
               .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
               .AddEnvironmentVariables();

            // Force critical defaults for Test (safety net)
            var forced = new Dictionary<string, string?>
            {
                ["Api:BasePath"] = "/api",
                ["Api:Version"] = "v1",
            };
            cfg.AddInMemoryCollection(forced);
        });

        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.SetMinimumLevel(LogLevel.Information);
        });

        builder.ConfigureServices(services =>
        {
            // Remove ANY prior EF registrations (pooled or not)
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType.FullName?.Contains("PooledDbContextFactory", StringComparison.OrdinalIgnoreCase) == true ||
                    d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            using var sp0 = services.BuildServiceProvider();
            var cfg = sp0.GetRequiredService<IConfiguration>();
            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in Test.");

            // Mirror the production registration style (pool or non-pool). If production uses AddDbContextPool, prefer it here too:
            services.AddDbContextPool<ApplicationDbContext>(o => o.UseNpgsql(cs));
            // If your Program.cs uses non-pooled: use AddDbContext instead:
            // services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(cs));

            // Apply migrations once
            using (var sp = services.BuildServiceProvider())
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }

            // Resolve API version segment from ApiExplorer (e.g., "v1" or "v1.0")
            using (var sp = services.BuildServiceProvider())
            {
                var provider = sp.GetService<IApiVersionDescriptionProvider>();
                if (provider != null)
                {
                    ApiVersionSegment = provider.ApiVersionDescriptions
                        .OrderBy(d => d.ApiVersion.MajorVersion)
                        .ThenBy(d => d.ApiVersion.MinorVersion)
                        .First().GroupName ?? "v1";
                }
            }

            // Route dump for CI diagnostics
            services.AddHostedService<RouteLogger>();
        });
    }

    // HostedService for logging mapped endpoints at test host startup
    private sealed class RouteLogger : IHostedService
    {
        private readonly ILogger<RouteLogger> _logger;
        private readonly EndpointDataSource _endpoints;

        public RouteLogger(ILogger<RouteLogger> logger, EndpointDataSource endpoints)
        {
            _logger = logger;
            _endpoints = endpoints;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var e in _endpoints.Endpoints)
            {
                if (e is RouteEndpoint re)
                    _logger.LogInformation("[TEST HOST] Mapped endpoint: {Route}", re.RoutePattern.RawText);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
