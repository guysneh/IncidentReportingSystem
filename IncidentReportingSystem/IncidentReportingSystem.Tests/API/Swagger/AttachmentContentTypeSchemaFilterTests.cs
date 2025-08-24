using FluentAssertions;
using IncidentReportingSystem.API.Swagger;
using IncidentReportingSystem.Infrastructure.Attachments;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Xunit;

namespace IncidentReportingSystem.Tests.API.Swagger;

[Trait("Category", "Unit")]
public sealed class AttachmentContentTypeSchemaFilterTests
{
    private static OpenApiSchema MakeSchemaWithContentType()
        => new()
        {
            Properties = { ["contentType"] = new OpenApiSchema { Type = "string" } }
        };

    [Fact(DisplayName = "Populates enum when AllowedContentTypes exist")]
    public void Applies_Enum_When_Configured()
    {
        var opts = Options.Create(new AttachmentOptions
        {
            AllowedContentTypes = new[] { "image/png", "image/jpeg" }
        });

        var filter = new AttachmentContentTypeSchemaFilter(opts);
        var schema = MakeSchemaWithContentType();

        filter.Apply(schema, context: null!);

        var prop = schema.Properties["contentType"];
        prop.Enum.Should().NotBeNull();
        prop.Enum!.Count.Should().Be(2);
    }

    [Fact(DisplayName = "Safe when AllowedContentTypes is empty")]
    public void Safe_When_Empty_List()
    {
        var opts = Options.Create(new AttachmentOptions
        {
            // explicitly empty so the filter sees no values
            AllowedContentTypes = Array.Empty<string>()
        });

        var filter = new AttachmentContentTypeSchemaFilter(opts);
        var schema = MakeSchemaWithContentType();

        filter.Apply(schema, context: null!);

        var prop = schema.Properties["contentType"];
        prop.Enum.Should().BeNullOrEmpty(); // no enum when list is empty
    }

    [Fact(DisplayName = "Safe when AllowedContentTypes is null")]
    public void Safe_When_Null_List()
    {
        var opts = Options.Create(new AttachmentOptions
        {
            // explicitly null to simulate no configuration bound
            AllowedContentTypes = null
        });

        var filter = new AttachmentContentTypeSchemaFilter(opts);
        var schema = MakeSchemaWithContentType();

        filter.Apply(schema, context: null!);

        var prop = schema.Properties["contentType"];
        prop.Enum.Should().BeNullOrEmpty(); // no enum when list is null
    }
}
