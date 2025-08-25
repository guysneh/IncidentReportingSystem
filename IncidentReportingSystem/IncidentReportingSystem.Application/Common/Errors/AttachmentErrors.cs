namespace IncidentReportingSystem.Application.Common.Errors
{
    /// <summary>Centralized error messages for attachments.</summary>
    public static class AttachmentErrors
    {
        public const string ParentNotFound = "The specified parent entity was not found.";
        public const string AttachmentNotFound = "Attachment not found.";
        public const string AttachmentNotPending = "Attachment is not in a pending state.";
        public const string UploadedObjectMissing = "Uploaded object not found in storage.";
        public const string InvalidFileSize = "Invalid file size.";
        public const string ContentTypeMismatch = "Stored ContentType does not match the declared ContentType.";
        public const string ContentTypeNotAllowed = "ContentType not allowed.";
        public const string AttachmentNotCompleted = "Attachment is not completed.";
    }
}
