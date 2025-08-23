using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using IncidentReportingSystem.Infrastructure.Authentication;
using IncidentReportingSystem.Infrastructure.Auth;

namespace IncidentReportingSystem.IntegrationTests.Infrastructure.Authentication
{
    [Trait("Category", "Integration")]
    public sealed class PasswordHasherPBKDF2Tests
    {
        private static PasswordHasherPBKDF2 NewHasher(int iterations = 10_000, int salt = 16, int key = 32)
            => new PasswordHasherPBKDF2(Options.Create(new PasswordHashingOptions
            {
                Iterations = iterations,
                SaltSizeBytes = salt,
                KeySizeBytes = key
            }));

        [Fact]
        [Trait("Category", "Integration")]
        public void HashPassword_Throws_On_NullOrWhitespace()
        {
            var hasher = NewHasher();
            FluentActions.Invoking(() => hasher.HashPassword(null!)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => hasher.HashPassword("")).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => hasher.HashPassword("   ")).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Verify_Roundtrip_True_And_InvalidInputs_False()
        {
            var hasher = NewHasher();
            var (hash, salt) = hasher.HashPassword("s3cr3t");

            hasher.Verify("s3cr3t", hash, salt).Should().BeTrue();

            hasher.Verify(null!, hash, salt).Should().BeFalse();
            hasher.Verify("", hash, salt).Should().BeFalse();
            hasher.Verify("wrong", hash, salt).Should().BeFalse();
            hasher.Verify("s3cr3t", null!, salt).Should().BeFalse();
            hasher.Verify("s3cr3t", hash, null!).Should().BeFalse();
            hasher.Verify("s3cr3t", new byte[1], salt).Should().BeFalse();                 
            hasher.Verify("s3cr3t", hash, new byte[1]).Should().BeFalse();                
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Ctor_Throws_On_NonPositive_Options()
        {
            FluentActions.Invoking(() => NewHasher(iterations: 0)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => NewHasher(salt: 0)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => NewHasher(key: 0)).Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
