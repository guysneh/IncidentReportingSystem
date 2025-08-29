using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using IncidentReportingSystem.API.Converters;
using IncidentReportingSystem.API.Filters;
using IncidentReportingSystem.API.Swagger;
using IncidentReportingSystem.API.Swagger.Examples;
using IncidentReportingSystem.Application;
using IncidentReportingSystem.Application.Behaviors;
using IncidentReportingSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace IncidentReportingSystem.API.Extensions;

public static class WebApiExtensions
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IHostEnvironment env)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new EnumConverterFactory());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .ToDictionary(
                            e => e.Key,
                            e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
                        );

                    var result = new { error = "Validation failed", details = errors };
                    return new BadRequestObjectResult(result);
                };
            });

        services.AddEndpointsApiExplorer();
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services.AddSwaggerGen(c =>
        {
            if (env.IsProduction()) 
            {
                c.DocumentFilter<HideLoopbackDocumentFilter>();
            }
            c.SchemaFilter<AttachmentContentTypeSchemaFilter>();
            c.OperationFilter<LoopbackBinaryRequestFilter>();
            c.SupportNonNullableReferenceTypes();
            c.UseInlineDefinitionsForEnums();
            c.OperationFilter<RegisterUserExample>();
            c.OperationFilter<WhoAmIExample>();
            c.OperationFilter<AttachmentsListExample>();
            c.OperationFilter<ProblemDetailsExample>();
            c.MapType<IncidentSeverity>(() => new OpenApiSchema
            {
                Type = "string",
                Enum = Enum.GetNames(typeof(IncidentSeverity))
                           .Select(v => (IOpenApiAny)new OpenApiString(v))
                           .ToList()
            });

            c.MapType<IncidentCategory>(() => new OpenApiSchema
            {
                Type = "string",
                Enum = Enum.GetNames(typeof(IncidentCategory))
                           .Select(v => (IOpenApiAny)new OpenApiString(v))
                           .ToList()
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter **only** your JWT token. Do NOT include the word 'Bearer'.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddMediatR(cfg =>
        {
            var assemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => a.FullName != null &&
                            a.FullName.StartsWith("IncidentReportingSystem.", StringComparison.Ordinal))
                .ToArray();

            cfg.RegisterServicesFromAssemblies(assemblies);
        });
        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyReference).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration cfg)
    {
        // Support both array and comma-separated string
        var fromArray = cfg.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var fromCsv = (cfg["Cors:AllowedOrigins"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var allowed = fromArray.Concat(fromCsv)
                               .Select(o => o.Trim().TrimEnd('/')) // normalize trailing slash
                               .Where(o => !string.IsNullOrWhiteSpace(o))
                               .Distinct(StringComparer.OrdinalIgnoreCase)
                               .ToArray();

        services.AddCors(options =>
        {
            options.AddPolicy("Default", b =>
            {
                if (allowed.Length > 0)
                {
                    b.WithOrigins(allowed)
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
                }
                else
                {
                    // Fallback (no credentials when wildcard)
                    b.SetIsOriginAllowed(_ => true)
                     .AllowAnyHeader()
                     .AllowAnyMethod();
                    // intentionally NOT calling .AllowCredentials() here
                }
            });
        });

        return services;
    }

}
