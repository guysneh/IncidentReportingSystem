using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Logging;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Features.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.DevLoopback;
using IncidentReportingSystem.Infrastructure.Attachments.Fake;
using IncidentReportingSystem.Infrastructure.Attachments.Services;
using IncidentReportingSystem.Infrastructure.Logging;
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
            services.AddSingleton<ISignedUrlService, SignedUrlService>();
            services.AddSingleton<IAttachmentAuditService, AttachmentAuditService>();

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

            services.PostConfigure<AttachmentOptions>(o =>
            {
                // ensure sane defaults
                if (o.MaxSizeBytes <= 0) o.MaxSizeBytes = 10 * 1024 * 1024;
                if (o.SasMinutesToLive <= 0) o.SasMinutesToLive = 15;

                // dedupe, keep stable order
                if (o.AllowedContentTypes != null)
                    o.AllowedContentTypes = o.AllowedContentTypes
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                else
                    o.AllowedContentTypes = new List<string>();

                if (o.AllowedExtensions != null)
                    o.AllowedExtensions = o.AllowedExtensions
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                else
                    o.AllowedExtensions = new List<string>();
            });

            return services;
        }
    }
}
