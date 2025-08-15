using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using IncidentReportingSystem.Domain.Auth;

public static class TestClientRoles
{
    public static HttpClient AsUser(this WebApplicationFactory<Program> f) =>
        AuthenticatedHttpClientFactory.CreateClientWithToken(f, roles: new[] { Roles.User });

    public static HttpClient AsAdmin(this WebApplicationFactory<Program> f) =>
        AuthenticatedHttpClientFactory.CreateClientWithToken(f, roles: new[] { Roles.Admin });

    public static HttpClient AsAnonymous(this WebApplicationFactory<Program> f) =>
        f.CreateClient();
}
