using System.Linq;
using System.Security.Claims;
using IncidentReportingSystem.API.Common;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.API.Common;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetEmail_Prefers_EmailClaim_Over_IdentityName()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("email", "e@x.com"),
            new Claim(ClaimTypes.Name, "name-will-be-ignored")
        }, "test");

        var principal = new ClaimsPrincipal(identity);

        var email = principal.GetEmail();
        Assert.Equal("e@x.com", email);
    }

    [Fact]
    public void GetEmail_FallsBack_To_IdentityName_When_No_Email_Claim()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "fallback@x.com")
        }, "test");

        var principal = new ClaimsPrincipal(identity);

        var email = principal.GetEmail();
        Assert.Equal("fallback@x.com", email);
    }

    [Fact]
    public void GetEmail_Returns_Empty_When_No_Email_And_No_Name()
    {
        var identity = new ClaimsIdentity(); // no claims
        var principal = new ClaimsPrincipal(identity);

        var email = principal.GetEmail();
        Assert.Equal(string.Empty, email);
    }

    [Fact]
    public void GetRoles_Aggregates_And_Distincts_Multiple_Claim_Types()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("role", "Auditor"),
            new Claim("roles", "Admin")
        }, "test");

        var principal = new ClaimsPrincipal(identity);

        var roles = principal.GetRoles().OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "Admin", "Auditor" }, roles);
    }
}
