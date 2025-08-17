using FluentAssertions;
using IncidentReportingSystem.Application.Authentication;
using IncidentReportingSystem.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace IncidentReportingSystem.Tests.Application
{
    /// <summary>
    /// Unit tests for PBKDF2 password hashing and verification.
    /// Verifies success on correct password, failure on wrong password,
    /// and different hashes for identical passwords due to unique salt.
    /// </summary>
    public sealed class PasswordHasherPBKDF2Tests
    {
        private static PasswordHasherPBKDF2 CreateHasher(int iterations = 200_000, int saltBytes = 16, int keyBytes = 32)
        {
            var opts = Options.Create(new PasswordHashingOptions
            {
                Iterations = iterations,
                SaltSizeBytes = saltBytes,
                KeySizeBytes = keyBytes
            });
            return new PasswordHasherPBKDF2(opts);
        }

        [Fact]
        public void Hash_And_Verify_Succeeds_For_Correct_Password()
        {
            var hasher = CreateHasher();
            var password = "P@ssw0rd!";

            var (hash, salt) = hasher.HashPassword(password);
            hash.Should().NotBeNullOrEmpty();
            salt.Should().NotBeNullOrEmpty();

            var ok = hasher.Verify(password, hash, salt);
            ok.Should().BeTrue();
        }

        [Fact]
        public void Verify_Fails_For_Wrong_Password()
        {
            var hasher = CreateHasher();
            var (hash, salt) = hasher.HashPassword("correct");

            var ok = hasher.Verify("wrong", hash, salt);
            ok.Should().BeFalse();
        }

        [Fact]
        public void Same_Password_Produces_Different_Hashes_Due_To_Salt()
        {
            var hasher = CreateHasher();
            var password = "SamePassword#1";

            var (h1, s1) = hasher.HashPassword(password);
            var (h2, s2) = hasher.HashPassword(password);

            s1.Should().NotEqual(s2);
            h1.Should().NotEqual(h2);
        }
    }
}
