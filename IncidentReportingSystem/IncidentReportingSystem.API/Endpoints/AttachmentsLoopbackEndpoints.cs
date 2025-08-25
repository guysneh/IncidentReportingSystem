using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Infrastructure.Attachments.DevLoopback;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace IncidentReportingSystem.API.Endpoints;

public static class AttachmentsLoopbackEndpoints
{
    /// <summary>
    /// Maps dev/test loopback upload endpoints that write directly to disk via LoopbackAttachmentStorage.
    /// </summary>
    public static IEndpointRouteBuilder MapAttachmentsLoopback(this IEndpointRouteBuilder app)
    {
        // PUT /api/{version}/attachments/_loopback/upload?path=...
        app.MapPut("/api/{version:apiVersion}/attachments/_loopback/upload",
            async (HttpRequest req,
                    string path,
                    LoopbackAttachmentStorage storage,
                    CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(path))
                    return Results.BadRequest();

                var contentType = req.ContentType ?? "application/octet-stream";
                await storage.ReceiveUploadAsync(path, req.Body, contentType, ct);
                return Results.StatusCode(StatusCodes.Status201Created);
            })
           .RequireAuthorization()
           .WithTags("Attachments")
           .ExcludeFromDescription(); 

        // POST /api/{version}/attachments/_loopback/upload-form  (multipart form-data)
        app.MapPost("/api/{version:apiVersion}/attachments/_loopback/upload-form",
            async (HttpRequest req,
                    LoopbackAttachmentStorage storage,
                    CancellationToken ct) =>
            {
                if (!req.HasFormContentType)
                    return Results.BadRequest();

                var form = await req.ReadFormAsync(ct);
                var path = form["path"].ToString();
                var file = form.Files["file"];
                if (string.IsNullOrWhiteSpace(path) || file is null)
                    return Results.BadRequest();

                await storage.ReceiveUploadAsync(path, file, ct);
                return Results.StatusCode(StatusCodes.Status201Created);
            })
           .RequireAuthorization()
           .WithTags("Attachments")
           .ExcludeFromDescription();

        return app;
    }
}
