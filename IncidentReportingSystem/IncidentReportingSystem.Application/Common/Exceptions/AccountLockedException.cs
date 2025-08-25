namespace IncidentReportingSystem.Application.Common.Exceptions;

/// <summary>
/// Thrown when a login attempt is made for an account currently under lockout.
/// </summary>
public sealed class AccountLockedException : Exception
{
    public DateTimeOffset? LockoutEndUtc { get; }

    public AccountLockedException(DateTimeOffset? lockoutEndUtc)
        : base(lockoutEndUtc.HasValue
            ? $"Account is locked until {lockoutEndUtc:O}."
            : "Account is locked.")
    {
        LockoutEndUtc = lockoutEndUtc;
    }
}
