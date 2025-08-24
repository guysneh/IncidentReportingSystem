using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Infrastructure.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentReportingSystem.API.Extensions
{
    /// <summary>Registers the current-user accessor (reads from HttpContext).</summary>
    public static class CurrentUserServiceExtensions
    {
        public static IServiceCollection AddCurrentUserAccessor(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            return services;
        }
    }
}
