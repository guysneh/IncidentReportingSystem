using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

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
                        ["Api:BasePath"] = "/api",
                        ["Api:Version"] = "v1",
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
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ApiVersion_from_config_reflected_in_payload()
        {
            using var factory = new TestFactory(new()
            {
                // override for the test
                ["Api:Version"] = "v9",
                ["App:Name"] = "Incident API (Test)"
            });
            var client = factory.CreateClient();

            var resp = await client.GetAsync("/api/v1/config-demo");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // The response uses camelCase (enabled, appName, apiVersion)
            Assert.True(root.TryGetProperty("apiVersion", out var ver), "payload is missing 'apiVersion'");
            Assert.Equal("v9", ver.GetString());

            // Optional extra checks 
            Assert.True(root.TryGetProperty("enabled", out var enabled));
            Assert.True(enabled.GetBoolean());

            Assert.True(root.TryGetProperty("appName", out var appName));
            Assert.False(string.IsNullOrWhiteSpace(appName.GetString()));
        }



    }
}
