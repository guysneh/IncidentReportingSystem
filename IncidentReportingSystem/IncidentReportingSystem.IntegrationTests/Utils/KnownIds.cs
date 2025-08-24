using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public static class KnownIds
{
    /// <summary>Reads Tests:ExistingIncidentId from configuration. Throws with a clear message if missing/invalid.</summary>
    public static Guid ExistingIncidentId(CustomWebApplicationFactory f)
    {
        using var scope = f.Services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var value = cfg["Tests:ExistingIncidentId"];

        if (Guid.TryParse(value, out var id))
            return id;

        throw new InvalidOperationException(
            "Missing or invalid Tests:ExistingIncidentId in appsettings.Test.json. " +
            "Set it to an existing IncidentReport ID so Start endpoint returns 200 instead of 404.");
    }
}
