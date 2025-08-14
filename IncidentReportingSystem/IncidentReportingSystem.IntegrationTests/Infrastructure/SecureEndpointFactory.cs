using IncidentReportingSystem.IntegrationTests.TestServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentReportingSystem.IntegrationTests.Infrastructure
{
    public sealed class SecureEndpointFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var baseConfig = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=postgres;Password=postgres",
                    ["Jwt:Issuer"] = "https://test.example",
                    ["Jwt:Audience"] = "test-clients",
                    ["Jwt:Secret"] = "TEST-SECRET-MUST-BE-AT-LEAST-32-CHARS-LONG"
                };
                cfg.AddInMemoryCollection(baseConfig!);
            });

            // ✅ Register the test controller assembly as an application part
            builder.ConfigureServices(services =>
            {
                var manager = services.AddControllers().PartManager;
                manager.ApplicationParts.Add(new AssemblyPart(typeof(TestSecureController).Assembly));
            });
        }
    }
}
