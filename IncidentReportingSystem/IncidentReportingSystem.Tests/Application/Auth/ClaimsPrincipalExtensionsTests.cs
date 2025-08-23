using System;
using System.Linq;
using System.Security.Claims;
using IncidentReportingSystem.API.Auth;
using Xunit;

namespace IncidentReportingSystem.Tests.Auth
{
    public class ClaimsPrincipalExtensionsTests
    {
        private static ClaimsPrincipal BuildPrincipal((string type, string value)[] claims)
        {
            var identity = new ClaimsIdentity(claims.Select(c => new Claim(c.type, c.value)), "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public void RequireUserId_Uses_NameIdentifier_WhenPresent()
        {
            var id = Guid.NewGuid();
            var principal = BuildPrincipal(new[] { (ClaimTypes.NameIdentifier, id.ToString()) });

            Guid result = principal.RequireUserId();

            Assert.Equal(id, result);
        }

        [Fact]
        public void RequireUserId_Uses_Sub_AsFallback()
        {
            var id = Guid.NewGuid();
            var principal = BuildPrincipal(new[] { ("sub", id.ToString()) });

            Guid result = principal.RequireUserId();

            Assert.Equal(id, result);
        }

        [Fact]
        public void RequireUserId_Throws_IfMissingOrNotGuid()
        {
            var principal = BuildPrincipal(Array.Empty<(string, string)>());

            Assert.Throws<InvalidOperationException>(() => principal.RequireUserId());

            var bad = BuildPrincipal(new[] { ("userId", "not-a-guid") });
            Assert.Throws<InvalidOperationException>(() => bad.RequireUserId());
        }

        [Fact]
        public void IsAdminRole_Detects_RoleClaims_And_IsInRole()
        {
            // Role via standard claim type
            var p1 = BuildPrincipal(new[] { (ClaimTypes.Role, "Admin") });
            Assert.True(p1.IsAdminRole());

            // Role via "role" type
            var p2 = BuildPrincipal(new[] { ("role", "Admin") });
            Assert.True(p2.IsAdminRole());

            // Non-admin role
            var p3 = BuildPrincipal(new[] { (ClaimTypes.Role, "User") });
            Assert.False(p3.IsAdminRole());

            // No roles
            var p4 = BuildPrincipal(Array.Empty<(string, string)>());
            Assert.False(p4.IsAdminRole());
        }
    }
}
