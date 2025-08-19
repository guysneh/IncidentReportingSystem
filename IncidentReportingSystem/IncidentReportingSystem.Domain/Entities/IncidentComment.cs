namespace IncidentReportingSystem.Domain.Entities
{
    /// <summary>
    /// Domain entity representing a single user-authored comment attached to an incident.
    /// </summary>
    public class IncidentComment
    {
        /// <summary>Primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>Incident this comment belongs to.</summary>
        public Guid IncidentId { get; set; }

        /// <summary>Authoring user id (immutable).</summary>
        public Guid UserId { get; set; }

        /// <summary>Plain-text content, validated in the Application layer.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>UTC timestamp of creation (set by the Application layer).</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>Optional navigation to the incident.</summary>
        public IncidentReport? Incident { get; set; }
    }
}