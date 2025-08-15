namespace IncidentReportingSystem.Domain.Interfaces
{
    /// <summary>
    /// Abstraction for committing changes to the persistence store.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
