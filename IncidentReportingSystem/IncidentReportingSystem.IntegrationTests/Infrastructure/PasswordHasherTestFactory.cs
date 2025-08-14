using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IncidentReportingSystem.IntegrationTests.Infrastructure
{
    public sealed class PasswordHasherTestFactory : WebApplicationFactory<Program>
    {
        // ✅ single public parameterless ctor (required by xUnit IClassFixture)
        public PasswordHasherTestFactory() { }

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
        }
    }
}
