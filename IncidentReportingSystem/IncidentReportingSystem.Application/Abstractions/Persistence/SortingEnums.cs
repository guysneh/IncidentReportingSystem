namespace IncidentReportingSystem.Application.Persistence
{
    /// <summary>Allowed fields for sorting incident reports.</summary>
    public enum IncidentSortField
    {
        /// <summary>Sort by the entity creation timestamp.</summary>
        CreatedAt = 0,
        /// <summary>Sort by the time the incident was reported.</summary>
        ReportedAt = 1,
        /// <summary>Sort by severity.</summary>
        Severity = 2,
        /// <summary>Sort by status.</summary>
        Status = 3
    }

    /// <summary>Sort direction (ascending / descending).</summary>
    public enum SortDirection
    {
        /// <summary>Ascending order.</summary>
        Asc = 0,
        /// <summary>Descending order.</summary>
        Desc = 1
    }
}