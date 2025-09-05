using static IncidentReportingSystem.UI.Core.Auth.AuthModels;

namespace IncidentReportingSystem.UI.Core.Auth;

public interface IAuthService
{
    Task<bool> SignInAsync(string email, string password, CancellationToken ct = default);
    Task RegisterAsync(string email, string password, string role, string first, string last, CancellationToken ct = default);
    Task SignOutAsync(CancellationToken ct = default);
    Task ChangePasswordAsync(string current, string @new, CancellationToken ct = default);
    Task UpdateMeAsync(string first, string last, CancellationToken ct = default);
    Task<AuthModels.WhoAmI?> MeAsync(CancellationToken ct = default);
}
