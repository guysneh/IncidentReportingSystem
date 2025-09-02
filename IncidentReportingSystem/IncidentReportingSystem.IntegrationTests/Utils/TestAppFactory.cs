// tests/IncidentReportingSystem.Tests.Integration/Infrastructure/TestAppFactory.cs
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Boots the API for integration tests using ONLY appsettings.Test.json,
    /// forces Test environment, overrides any __PORT__ placeholders, uses TestServer,
    /// and applies EF migrations on the Test DB.
    /// </summary>
    public sealed class TestAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSolutionRelativeContentRoot("IncidentReportingSystem.API");
            builder.UseEnvironment("Test");
            builder.UseTestServer();

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.Sources.Clear();

                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                cfg.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false);
                cfg.AddEnvironmentVariables();

                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["urls"] = "http://127.0.0.1:0",
                    ["Kestrel:Endpoints:Http:Url"] = "http://127.0.0.1:0",
                    ["Kestrel:Endpoints:Http:Port"] = "0",
                    ["Kestrel:Endpoints:Https:Url"] = "https://127.0.0.1:0",
                    ["Kestrel:Endpoints:Https:Port"] = "0",
                    ["PORT"] = "0",
                    ["ASPNETCORE_URLS"] = "http://127.0.0.1:0"
                });
            });
            builder.ConfigureServices(services =>
            {
                var existing = services.FirstOrDefault(d => d.ServiceType == typeof(ICurrentUserService));
                if (existing is not null) services.Remove(existing);

                services.AddSingleton<ICurrentUserService>(_ =>
                    new TestCurrentUserService(new Guid("00000000-0000-0000-0000-000000000001")));
            });
        }

        public async Task InitializeAsync()
        {
            using var _ = CreateClient();

            using var scope = Services.CreateScope();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            Debug.WriteLine("[IT] urls=" + (cfg["urls"] ?? "(null)"));
            Debug.WriteLine("[IT] Kestrel:Endpoints:Http:Port=" + (cfg["Kestrel:Endpoints:Http:Port"] ?? "(null)"));
            Debug.WriteLine("[IT] ASPNETCORE_URLS=" + (System.Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "(env null)"));

            var db = scope.ServiceProvider.GetRequiredService<IncidentReportingSystem.Infrastructure.Persistence.ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
