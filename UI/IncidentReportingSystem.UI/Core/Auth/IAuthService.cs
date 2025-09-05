using static IncidentReportingSystem.UI.Core.Auth.AuthModels;

namespace IncidentReportingSystem.UI.Core.Auth;

public interface IAuthService
{
    Task<bool> SignInAsync(string email, string password, CancellationToken ct = default);
    Task<bool> RegisterAsync(string email, string password, string role, string firstName, string lastName, CancellationToken ct = default); Task<WhoAmI?> MeAsync(CancellationToken ct = default);
    Task SignOutAsync(CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);
    Task<bool> UpdateMeAsync(string firstName, string lastName, CancellationToken ct = default);
}
