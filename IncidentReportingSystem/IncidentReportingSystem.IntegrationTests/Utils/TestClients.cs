using System.Net.Http;
using System.Threading.Tasks;

namespace IncidentReportingSystem.IntegrationTests.Utils;

/// <summary>
/// Unified helpers to obtain authenticated HttpClient for tests.
/// Wraps existing AuthTestHelpers without changing global auth configuration.
/// </summary>
public static class TestClients
{
    public static Task<HttpClient> AsUserAsync(
        CustomWebApplicationFactory factory,
        string[]? roles = null,
        string? email = null,
        string password = "P@ssw0rd!")
        => AuthTestHelpers.RegisterAndLoginAsync(
            factory,
            roles: roles,
            email: email,
            password: password
        );
}
