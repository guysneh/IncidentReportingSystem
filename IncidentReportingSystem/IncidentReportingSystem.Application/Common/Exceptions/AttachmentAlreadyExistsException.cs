namespace IncidentReportingSystem.Application.Common.Exceptions
{
    /// <summary>Conflict: target object already exists in storage.</summary>
    public sealed class AttachmentAlreadyExistsException : Exception
    {
        public AttachmentAlreadyExistsException(string message) : base(message) { }
    }
}