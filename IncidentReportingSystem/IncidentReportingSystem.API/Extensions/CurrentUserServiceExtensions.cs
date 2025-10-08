using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Infrastructure.Auth;
using IncidentReportingSystem.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentReportingSystem.API.Extensions
{
    /// <summary>Registers the current-user accessor (reads from HttpContext).</summary>
    public static class CurrentUserServiceExtensions
    {
        public static IServiceCollection AddCurrentUserAccessor(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            return services;
        }
    }
}
