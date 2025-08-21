namespace IncidentReportingSystem.Application.Exceptions
{
    /// <summary>
    /// Thrown when email/password combination is invalid. Mapped to 401 by exception middleware.
    /// </summary>
    public sealed class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("Invalid email or password.") { }
    }
}