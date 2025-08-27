namespace IncidentReportingSystem.API.Contracts.Paging
{
    /// <summary>
    /// Generic API response envelope for paged lists. Mirrors Application's PagedResult.
    /// </summary>
    public sealed class PagedResponse<T>
    {
        public int Total { get; init; }
        public int Skip { get; init; }
        public int Take { get; init; }
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    }
}
