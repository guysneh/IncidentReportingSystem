namespace IncidentReportingSystem.UI.Core.Auth;

public sealed class AuthBootstrapper
{
    public AuthBootstrapper(AuthSessionStore store, AuthState state)
    {
        // Try to rehydrate from cookie at circuit start
        store.TryLoadInto(state);
    }
}
