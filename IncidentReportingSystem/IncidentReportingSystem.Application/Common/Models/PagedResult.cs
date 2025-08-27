namespace IncidentReportingSystem.Application.Common.Models
{
    /// <summary>
    /// Generic, application-layer paging envelope.
    /// </summary>
    public sealed class PagedResult<T>
    {
        /// <summary>Total number of items that match the filter (ignores paging).</summary>
        public int Total { get; }
        /// <summary>Zero-based offset used for this page.</summary>
        public int Skip { get; }
        /// <summary>Number of items requested for this page.</summary>
        public int Take { get; }
        /// <summary>Current page items.</summary>
        public IReadOnlyList<T> Items { get; }

        public PagedResult(IReadOnlyList<T> items, int total, int skip, int take)
        {
            Items = items ?? Array.Empty<T>();
            Total = total;
            Skip = skip;
            Take = take;
        }
    }
}
