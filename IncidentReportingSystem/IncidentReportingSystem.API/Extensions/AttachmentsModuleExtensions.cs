using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Infrastructure.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.DevLoopback;
using IncidentReportingSystem.Infrastructure.Attachments.Fake;
using IncidentReportingSystem.Infrastructure.Attachments.Services;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IncidentReportingSystem.API.Extensions
{
    /// <summary>Registers attachment module services and the chosen storage implementation.</summary>
    public static class AttachmentsModuleExtensions
    {
        public static IServiceCollection AddAttachmentsModule(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment? env = null)
        {
            services.Configure<AttachmentOptions>(configuration.GetSection("Attachments"));
            services.AddScoped<IAttachmentPolicy, AttachmentPolicy>();
            services.AddScoped<IAttachmentRepository, AttachmentRepository>();
            services.AddScoped<IAttachmentParentReadService, AttachmentParentReadService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            var storage = configuration["Attachments:Storage"];
            var useLoopback = string.Equals(storage, "Loopback", StringComparison.OrdinalIgnoreCase)
                              || (string.IsNullOrWhiteSpace(storage) && env?.IsDevelopment() == true);

            if (useLoopback)
            {
                services.AddSingleton<LoopbackAttachmentStorage>();
                services.AddSingleton<IAttachmentStorage>(sp => sp.GetRequiredService<LoopbackAttachmentStorage>());
            }
            else
            {
                services.AddScoped<IAttachmentStorage, FakeAttachmentStorage>();
                services.AddScoped<LoopbackAttachmentStorage>();
            }

            return services;
        }
    }
}
