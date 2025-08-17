using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace IncidentReportingSystem.IntegrationTests.Configuration
{
    public class ConfigurationProbeTests
    {
        private class TestFactory : WebApplicationFactory<Program>
        {
            private readonly Dictionary<string, string?> _overrides;

            public TestFactory(Dictionary<string, string?>? overrides = null)
            {
                _overrides = overrides ?? new();
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["AppConfig:Enabled"] = "false",          // default: do not wire AppConfig in tests
                        ["AppConfig:Endpoint"] = "",
                        ["Demo:EnableConfigProbe"] = "false",
                        ["Api:BasePath"] = "/api",
                        ["Api:Version"] = "v1",
                        ["EnableSwagger"] = "true"                // allow swagger in Test env
                    });

                    if (_overrides.Count > 0)
                        cfg.AddInMemoryCollection(_overrides);
                });
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Health_live_returns_ok()
        {
            using var factory = new TestFactory();
            var client = factory.CreateClient();

            var resp = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ConfigProbe_is_hidden_by_default_in_non_dev()
        {
            using var factory = new TestFactory();
            var client = factory.CreateClient();

            var resp = await client.GetAsync("/api/v1/config-demo");
            Assert.True(
                resp.StatusCode == HttpStatusCode.NotFound ||
                resp.StatusCode == HttpStatusCode.Unauthorized,
                $"Expected 404 or 401, got {resp.StatusCode}"
            );
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ApiVersion_from_config_changes_route()
        {
            using var factory = new TestFactory(new()
            {
                ["Demo:EnableConfigProbe"] = "true",
                ["Api:Version"] = "v9"
            });
            var client = factory.CreateClient();

            // old version should not be accessible for anonymous
            var wrong = await client.GetAsync("/api/v1/config-demo");
            Assert.True(
                wrong.StatusCode == HttpStatusCode.NotFound ||
                wrong.StatusCode == HttpStatusCode.Unauthorized,
                $"Expected 404 or 401 for old version, got {wrong.StatusCode}"
            );

            // new version route exists but requires auth -> 401 for anonymous
            var correct = await client.GetAsync("/api/v9/config-demo");
            Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
        }

    }
}
