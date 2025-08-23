using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Infrastructure.Auth;
using IncidentReportingSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    public sealed class PasswordHasherIntegrationTests : IClassFixture<PasswordHasherTestFactory>
    {
        private readonly PasswordHasherTestFactory _factory;

        public PasswordHasherIntegrationTests(PasswordHasherTestFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void DI_Resolves_IPasswordHasher_And_Hash_Verify_Works()
        {
            using var scope = _factory.Services.CreateScope();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var (hash, salt) = hasher.HashPassword("Passw0rd!");

            Assert.NotNull(hash);
            Assert.NotNull(salt);
            Assert.True(hash.Length >= 16); // default is 32
            Assert.True(salt.Length >= 8);  // default is 16

            Assert.True(hasher.Verify("Passw0rd!", hash, salt));
            Assert.False(hasher.Verify("Wrong!", hash, salt));
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Options_Bind_From_Environment_Variables()
        {
            // Arrange env vars (double underscore => nested keys)
            using var env = new EnvironmentVariableScope(new Dictionary<string, string>
            {
                ["AUTH__PASSWORDHASHING__ITERATIONS"] = "123456",
                ["AUTH__PASSWORDHASHING__SALTSIZEBYTES"] = "20",
                ["AUTH__PASSWORDHASHING__KEYSIZEBYTES"] = "40"
            });

            // Build a fresh server to pick up env vars
            using var factory = new PasswordHasherTestFactory();
            using var scope = factory.Services.CreateScope();

            var opts = scope.ServiceProvider.GetRequiredService<IOptions<PasswordHashingOptions>>().Value;

            Assert.Equal(123456, opts.Iterations);
            Assert.Equal(20, opts.SaltSizeBytes);
            Assert.Equal(40, opts.KeySizeBytes);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Root_Redirects_To_Swagger_In_Development()
        {
            var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var resp = client.GetAsync("/").GetAwaiter().GetResult();
            Assert.True((int)resp.StatusCode is 301 or 302 or 307 or 308);
            Assert.True(resp.Headers.Location?.ToString().Contains("/swagger", StringComparison.OrdinalIgnoreCase));
        }
    }
}
