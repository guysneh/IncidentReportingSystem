namespace IncidentReportingSystem.Application.Abstractions.Security
{
    /// <summary>
    /// Abstraction for hashing and verifying passwords.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes the specified plaintext password using a secure KDF and a random salt.
        /// </summary>
        /// <param name="password">Plaintext password to hash.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A tuple containing the derived key (hash) and the generated salt.</returns>
        (byte[] Hash, byte[] Salt) HashPassword(string password, CancellationToken ct = default);

        /// <summary>
        /// Verifies that the provided plaintext password corresponds to the given hash and salt.
        /// </summary>
        /// <param name="password">Plaintext password to verify.</param>
        /// <param name="hash">Previously stored derived key.</param>
        /// <param name="salt">Previously stored salt.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if the password is valid; otherwise false.</returns>
        bool Verify(string password, byte[] hash, byte[] salt, CancellationToken ct = default);
    }
}
