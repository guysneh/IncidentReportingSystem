using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Infrastructure.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.Fake;
using IncidentReportingSystem.Infrastructure.Attachments.Services;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentReportingSystem.API.Extensions
{
    /// <summary>
    /// Registers all DI services required for the Attachments feature.
    /// Composition Root stays in API; Application remains free of Infra dependencies.
    /// </summary>
    public static class AttachmentsModuleExtensions
    {
        /// <summary>Adds the Attachments module services and options.</summary>
        public static IServiceCollection AddAttachmentsModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Options & policy
            services.Configure<AttachmentOptions>(configuration.GetSection("Attachments"));
            services.AddScoped<IAttachmentPolicy, AttachmentPolicy>();

            // Repositories / Read services / Unit of Work (EF-backed, Infrastructure layer)
            services.AddScoped<IAttachmentRepository, AttachmentRepository>();
            services.AddScoped<IAttachmentParentReadService, AttachmentParentReadService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Storage provider — Fake for now; later swap to AzureBlobAttachmentStorage
            services.AddScoped<IAttachmentStorage, FakeAttachmentStorage>();

            return services;
        }
    }
}
