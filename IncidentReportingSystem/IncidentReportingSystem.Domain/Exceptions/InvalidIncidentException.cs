namespace IncidentReportingSystem.Domain.Exceptions
{
    /// <summary>
    /// Exception indicating an invalid operation on an incident.
    /// </summary>
    public class InvalidIncidentOperationException : Exception
    {
        public InvalidIncidentOperationException() { }
        public InvalidIncidentOperationException(string message) : base(message) { }
        public InvalidIncidentOperationException(string message, Exception innerException) : base(message, innerException) { }

    }
}
