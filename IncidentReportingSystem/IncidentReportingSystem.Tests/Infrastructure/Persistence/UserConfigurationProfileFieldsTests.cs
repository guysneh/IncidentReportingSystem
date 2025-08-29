using System;
using FluentAssertions;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IncidentReportingSystem.Tests.Infrastructure.Persistence
{
    /// <summary>
    /// Verifies EF Core metadata for optional user profile fields
    /// (length constraints and nullability) without hitting a real database.
    /// </summary>
    public sealed class UserConfigurationProfileFieldsTests
    {
        /// <summary>
        /// Builds an in-memory <see cref="ApplicationDbContext"/> instance that applies
        /// the production EF model configuration so we can inspect metadata.
        /// </summary>
        private static ApplicationDbContext NewContext()
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

            return new ApplicationDbContext(opts);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void User_ProfileFields_Are_Nullable_With_Expected_MaxLengths()
        {
            using var ctx = NewContext();
            var entity = ctx.Model.FindEntityType(typeof(User));
            entity.Should().NotBeNull("User entity must be mapped");

            var firstName = entity!.FindProperty(nameof(User.FirstName));
            firstName.Should().NotBeNull();
            firstName!.IsNullable.Should().BeTrue();
            firstName.GetMaxLength().Should().Be(100);

            var lastName = entity.FindProperty(nameof(User.LastName));
            lastName.Should().NotBeNull();
            lastName!.IsNullable.Should().BeTrue();
            lastName.GetMaxLength().Should().Be(100);

            var displayName = entity.FindProperty(nameof(User.DisplayName));
            displayName.Should().NotBeNull();
            displayName!.IsNullable.Should().BeTrue();
            displayName.GetMaxLength().Should().Be(200);
        }
    }
}
