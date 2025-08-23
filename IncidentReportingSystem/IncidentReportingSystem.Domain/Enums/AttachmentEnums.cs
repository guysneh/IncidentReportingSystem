namespace IncidentReportingSystem.Domain.Enums
{
    /// <summary>Parent entity kinds an attachment can belong to.</summary>
    public enum AttachmentParentType { None = 0, Incident = 1, Comment = 2 }

    /// <summary>Lifecycle states of an attachment.</summary>
    public enum AttachmentStatus { Pending = 0, Completed = 1, Blocked = 2 }
}

