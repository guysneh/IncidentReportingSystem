namespace IncidentReportingSystem.Application.Common.Exceptions
{
    public sealed class EmailAlreadyExistsException : Exception
    {
        public string Email { get; }

        public EmailAlreadyExistsException(string email)
            : base($"A user with email '{email}' already exists.")
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }
    }
}
