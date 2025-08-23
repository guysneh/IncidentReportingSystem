using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;
using IncidentReportingSystem.Infrastructure.Auth;
using IncidentReportingSystem.Application.Common.Auth;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    [Trait("Category", "Integration")]
    public sealed class JwtTokenServiceTests
    {
        private static IConfiguration BuildConfig(Dictionary<string, string?> overrides)
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "irs.test",
                ["Jwt:Audience"] = "irs.audience",
                ["Jwt:Secret"] = "0123456789ABCDEF0123456789ABCDEF", // 32 bytes Base16
                ["Jwt:AccessTokenMinutes"] = "15"
            };

            foreach (var kv in overrides) dict[kv.Key] = kv.Value;

            return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Generate_Includes_Email_Name_Roles_And_ExtraClaims()
        {
            var cfg = BuildConfig(new());
            var svc = new JwtTokenService(cfg);

            var (token, expires) = svc.Generate(
                userId: "u1",
                roles: new[] { "Admin", "  ", null, "Operator" },   
                email: "user@test.local",
                extraClaims: new Dictionary<string, string?> { ["x"] = "1" } 
            );

            expires.Should().BeAfter(DateTime.UtcNow);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            jwt.Claims.Should().Contain(c => c.Type == ClaimTypesConst.UserId && c.Value == "u1");
            jwt.Claims.Should().Contain(c => c.Type == ClaimTypesConst.Email && c.Value == "user@test.local");
            jwt.Claims.Should().Contain(c => c.Type == ClaimTypesConst.Name && c.Value == "user@test.local");
            jwt.Claims.Count(c => c.Type == ClaimTypesConst.Role && (c.Value == "Admin" || c.Value == "Operator"))
                .Should().Be(2);
            jwt.Claims.Should().Contain(c => c.Type == "x" && c.Value == "1");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Generate_Without_Email_And_Roles_And_ExtraClaims()
        {
            var cfg = BuildConfig(new());
            var svc = new JwtTokenService(cfg);

            var (token, _) = svc.Generate("u2", roles: null, extraClaims: null);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            jwt.Claims.Should().ContainSingle(c => c.Type == ClaimTypesConst.UserId && c.Value == "u2");
            jwt.Claims.Any(c => c.Type == ClaimTypesConst.Email).Should().BeFalse();
            jwt.Claims.Any(c => c.Type == ClaimTypesConst.Role).Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Generate_Uses_Default60_When_AccessTokenMinutes_Invalid_Even_If_ExpirationMinutes_Set()
        {
            var cfg = BuildConfig(new()
            {
                ["Jwt:AccessTokenMinutes"] = "not-a-number",
                ["Jwt:ExpirationMinutes"] = "5"
            });
            var svc = new JwtTokenService(cfg);

            var (_, exp1) = svc.Generate("u3", roles: Array.Empty<string>(), email: null, extraClaims: null);
            (exp1 - DateTime.UtcNow).TotalMinutes.Should().BeGreaterThan(55).And.BeLessThan(65);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Generate_Uses_Default60_When_Both_Minutes_Invalid_Or_NonPositive()
        {
            var cfg = BuildConfig(new()
            {
                ["Jwt:AccessTokenMinutes"] = "-1",
                ["Jwt:ExpirationMinutes"] = "zero"
            });
            var svc = new JwtTokenService(cfg);

            var (_, exp2) = svc.Generate("u4", roles: Array.Empty<string>(), email: null, extraClaims: null);
            (exp2 - DateTime.UtcNow).TotalMinutes.Should().BeGreaterThan(55).And.BeLessThan(65);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Generate_Throws_When_Secret_Missing()
        {
            var cfg = BuildConfig(new() { ["Jwt:Secret"] = null });
            var svc = new JwtTokenService(cfg);

            Action act = () => svc.Generate("u5", null, null, null);
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*Jwt:Secret*");
        }
    }
}
