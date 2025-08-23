using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Infrastructure.Auth;
using IncidentReportingSystem.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace IncidentReportingSystem.Tests.Auth
{
    public class PasswordHasherPBKDF2Tests
    {
        private static IPasswordHasher CreateHasher(
            int saltLen = 16,
            int keyLen = 32,
            int iterations = 100_000)
        {
            var opts = new PasswordHashingOptions
            {
                SaltSizeBytes = saltLen,
                KeySizeBytes = keyLen,
                Iterations = iterations
            };
            return new PasswordHasherPBKDF2(Options.Create(opts));
        }

        [Fact]
        public void HashPassword_Returns_NonEmpty_Hash_And_Salt_With_Configured_Lengths()
        {
            // Arrange
            var hasher = CreateHasher(saltLen: 24, keyLen: 48);

            // Act
            var (hash, salt) = hasher.HashPassword("Str0ngP@ss!");

            // Assert
            Assert.NotNull(hash);
            Assert.NotNull(salt);
            Assert.Equal(48, hash.Length);
            Assert.Equal(24, salt.Length);
        }

        [Fact]
        public void HashPassword_Generates_Different_Salt_Each_Call()
        {
            // Arrange
            var hasher = CreateHasher();

            // Act
            var (_, salt1) = hasher.HashPassword("same-password");
            var (_, salt2) = hasher.HashPassword("same-password");

            // Assert
            Assert.NotEqual(salt1, salt2); 
        }

        [Fact]
        public void Verify_Returns_True_For_Correct_Password_And_False_For_Wrong()
        {
            // Arrange
            var hasher = CreateHasher();
            var password = "Str0ngP@ss!";
            var (hash, salt) = hasher.HashPassword(password);

            // Act + Assert
            Assert.True(hasher.Verify(password, hash, salt));
            Assert.False(hasher.Verify("wrong-password", hash, salt));
        }

        [Fact]
        public void HashPassword_Throws_On_Null_Or_Empty_Password()
        {
            // Arrange
            var hasher = CreateHasher();

            // Act + Assert 
            Assert.Throws<ArgumentNullException>(() => hasher.HashPassword(null!));
            Assert.Throws<ArgumentNullException>(() => hasher.HashPassword(string.Empty));
            Assert.Throws<ArgumentNullException>(() => hasher.HashPassword("   "));
        }

        [Fact]
        public void Constructor_Throws_On_Invalid_Options()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PasswordHasherPBKDF2(Options.Create(new PasswordHashingOptions
                {
                    SaltSizeBytes = 0,
                    KeySizeBytes = 32,
                    Iterations = 100_000
                })));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PasswordHasherPBKDF2(Options.Create(new PasswordHashingOptions
                {
                    SaltSizeBytes = 16,
                    KeySizeBytes = 0,
                    Iterations = 100_000
                })));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PasswordHasherPBKDF2(Options.Create(new PasswordHashingOptions
                {
                    SaltSizeBytes = 16,
                    KeySizeBytes = 32,
                    Iterations = 0
                })));
        }

        [Fact]
        public void Verify_Returns_False_When_Given_Null_Or_Wrong_Length_Arrays()
        {
            // Arrange
            var hasher = CreateHasher(saltLen: 16, keyLen: 32);
            var (hash, salt) = hasher.HashPassword("abc123!");

            // Sanity
            Assert.True(hasher.Verify("abc123!", hash, salt));

            // null inputs
            Assert.False(hasher.Verify("abc123!", null!, salt));
            Assert.False(hasher.Verify("abc123!", hash, null!));

            // wrong lengths
            Assert.False(hasher.Verify("abc123!", new byte[31], salt)); 
            Assert.False(hasher.Verify("abc123!", hash, new byte[15])); 
        }
    }
}
