using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.Application.Common.Logging;

/// <summary>
/// Centralized, strongly-typed EventId catalog for audit/monitoring logs.
/// Number ranges per bounded context make filtering and analytics predictable.
/// </summary>
public static class AuditEvents
{
    public static class Auth
    {
        /// <summary>User registration completed successfully.</summary>
        public static readonly EventId Registered = new(1001, nameof(Registered));

        /// <summary>User login completed successfully.</summary>
        public static readonly EventId Login = new(1002, nameof(Login));
    }

    public static class Attachments
    {
        /// <summary>Attachment upload start issued.</summary>
        public static readonly EventId Start = new(2001, nameof(Start));

        /// <summary>Attachment upload completion accepted.</summary>
        public static readonly EventId Complete = new(2002, nameof(Complete));

        /// <summary>Attachment download served.</summary>
        public static readonly EventId Download = new(2003, nameof(Download));
    }

    public static class Incidents
    {
        /// <summary>New incident created.</summary>
        public static readonly EventId Created = new(3001, nameof(Created));
    }

    public static class Comments
    {
        /// <summary>New comment created.</summary>
        public static readonly EventId Created = new(4001, nameof(Created));

        /// <summary>Comment deleted.</summary>
        public static readonly EventId Deleted = new(4002, nameof(Deleted));
    }
}
