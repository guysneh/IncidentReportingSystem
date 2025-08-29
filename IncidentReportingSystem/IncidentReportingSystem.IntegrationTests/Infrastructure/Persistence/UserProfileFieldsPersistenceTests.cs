using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Infrastructure.Persistence
{
    /// <summary>
    /// Persists a new <see cref="User"/> with optional profile fields and verifies
    /// round-trip persistence through the real EF Core pipeline used by the app.
    /// </summary>
    [Trait("Category", "Integration")]
    public sealed class UserProfileFieldsPersistenceTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UserProfileFieldsPersistenceTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Can_Persist_And_Read_User_With_ProfileFields()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var email = $"ui-profile-{Guid.NewGuid():N}@test.local";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                PasswordHash = Array.Empty<byte>(),
                PasswordSalt = Array.Empty<byte>(),
                FirstName = "Ada",
                LastName = "Lovelace",
                DisplayName = "Ada Lovelace"
            };
            user.SetRoles(new[] { "User" }); // reuse existing roles semantics

            // Act
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var reloaded = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == user.Id)
                .SingleAsync();

            // Assert
            reloaded.Email.Should().Be(email);
            reloaded.FirstName.Should().Be("Ada");
            reloaded.LastName.Should().Be("Lovelace");
            reloaded.DisplayName.Should().Be("Ada Lovelace");
            reloaded.Roles.Should().ContainSingle(r => string.Equals(r, "User", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Can_Persist_User_With_Null_ProfileFields_For_BackCompat()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var email = $"ui-profile-null-{Guid.NewGuid():N}@test.local";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                PasswordHash = Array.Empty<byte>(),
                PasswordSalt = Array.Empty<byte>(),
                FirstName = null,
                LastName = null,
                DisplayName = null
            };
            user.SetRoles(new[] { "User" });

            // Act
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);

            // Assert
            reloaded.FirstName.Should().BeNull();
            reloaded.LastName.Should().BeNull();
            reloaded.DisplayName.Should().BeNull();
        }
    }
}
