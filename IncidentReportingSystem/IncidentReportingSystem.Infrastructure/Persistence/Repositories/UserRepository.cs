using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Domain.Users;
using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of IUserRepository.
    /// NOTE: No SaveChanges here — UnitOfWork commits.
    /// </summary>
    public sealed class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;

        public UserRepository(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            var normalized = email.Trim().ToUpperInvariant();
            return _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
        }

        public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            _db.Users.AsNoTracking().AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken) =>
            _db.Users.AsNoTracking().AnyAsync(u => u.Id == id, cancellationToken);

        public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            _db.Users.AddAsync(user, cancellationToken).AsTask();
    }
}
