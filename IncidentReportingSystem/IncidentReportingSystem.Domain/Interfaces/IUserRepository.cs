using IncidentReportingSystem.Domain.Users;

namespace IncidentReportingSystem.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct);
        Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct);
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct);
        Task AddAsync(User user, CancellationToken ct);
        Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct);
    }
}
