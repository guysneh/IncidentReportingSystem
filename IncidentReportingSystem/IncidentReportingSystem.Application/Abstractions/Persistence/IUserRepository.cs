using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
        Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<User?> GetByEmailAsync(string email, CancellationToken ctcancellationToken);
        Task AddAsync(User user, CancellationToken ctcancellationToken);
        Task<bool> ExistsByIdAsync(Guid id, CancellationToken ctcancellationToken);
    }
}
