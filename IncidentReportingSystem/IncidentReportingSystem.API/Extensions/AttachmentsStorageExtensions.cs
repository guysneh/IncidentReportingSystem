using System;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.Storage;
using IncidentReportingSystem.Infrastructure.Attachments.Sanitization;

namespace IncidentReportingSystem.API.Extensions
{
    public static class AttachmentsStorageExtensions
    {
        public static IServiceCollection AddAttachmentsStorage(this IServiceCollection services, IConfiguration config)
        {
            var mode = config["Attachments:Storage"] ?? "Loopback"; // dev-debug stays Loopback
            var container = config["Attachments:Container"] ?? "attachments";
            var publicEndpoint = config["Storage:Blob:PublicEndpoint"];       // optional
            var ttlMinutesStr = config["Storage:Blob:UploadSasTtlMinutes"];  // optional
            var ttlMinutes = int.TryParse(ttlMinutesStr, out var m) && m > 0 ? m : 15;
            services.AddSingleton<IImageSanitizer, ImageSharpSanitizer>();
            if (mode.Equals("Azurite", StringComparison.OrdinalIgnoreCase))
            {
                var options = new AzureBlobAttachmentStorage.Options(
                    Endpoint: Require(config, "Storage:Blob:Endpoint"),
                    AccountName: Require(config, "Storage:Blob:AccountName"),
                    AccountKey: Require(config, "Storage:Blob:AccountKey"),
                    Container: container,
                    UploadSasTtl: TimeSpan.FromMinutes(ttlMinutes),
                    PublicEndpoint: publicEndpoint
                );

                services.AddSingleton(options);
                services.AddSingleton<IAttachmentStorage, AzureBlobAttachmentStorage>();
            }
            else if (mode.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                var options = new AzureBlobAttachmentStorage.Options(
                    Endpoint: Require(config, "Storage:Blob:Endpoint"),   // https://{account}.blob.core.windows.net
                    AccountName: Require(config, "Storage:Blob:AccountName"),
                    AccountKey: null,
                    Container: container,
                    UploadSasTtl: TimeSpan.FromMinutes(ttlMinutes),
                    PublicEndpoint: publicEndpoint 
                );

                services.AddSingleton(options);
                services.AddSingleton<IAttachmentStorage>(sp =>
                    new AzureBlobAttachmentStorage(options, new DefaultAzureCredential()));
            }
            // else: Loopback or any custom mode -> No-Op (preserve existing wiring)

            return services;
        }

        private static string Require(IConfiguration cfg, string key)
        {
            var value = cfg[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Missing required configuration key: {key}");
            return value;
        }
    }
}
