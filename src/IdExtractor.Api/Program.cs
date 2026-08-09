using System.Threading.RateLimiting;
using Asp.Versioning;
using IdExtractor.Api.Infrastructure.Auth;
using IdExtractor.Api.Infrastructure.Logging;
using IdExtractor.Api.Infrastructure.RateLimiting;
using IdExtractor.Modules.Documents;
using IdExtractor.Modules.Extraction;
using IdExtractor.SharedKernel.Auditing;
using IdExtractor.SharedKernel.Modules;
using IdExtractor.SharedKernel.Telemetry;
using Microsoft.AspNetCore.Authentication;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "IdExtractor.Api")
    .Destructure.With<SensitiveDestructuringPolicy>()
    .WriteTo.Console());

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("IdExtractor.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(IdExtractorActivitySource.Name)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        }
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, CurrentActor>();

builder.Services.Configure<ApiKeysOptions>(builder.Configuration.GetSection(ApiKeysOptions.SectionName));
builder.Services
    .AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.AuthenticationScheme, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimiterPolicies.PerApiKey, httpContext =>
    {
        var apiKey = httpContext.Request.Headers[ApiKeyDefaults.HeaderName].ToString();
        var partitionKey = string.IsNullOrEmpty(apiKey)
            ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : apiKey;

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// One OpenAPI document per API version. Add a new AddOpenApi("v2", ...) call (and a
// matching HasApiVersion(new ApiVersion(2)) below) when a second version ships.
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "ID Details Extractor API";
        document.Info.Version = "v1";
        document.Info.Description =
            "Extracts structured details from ID documents (national IDs, passports, driving licences).";
        return Task.CompletedTask;
    });
});

builder.Services.AddHealthChecks();

IModule[] modules = [new ExtractionModule(), new DocumentsModule()];

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(modules.Select(m => m.GetType().Assembly).Distinct().ToArray()));

foreach (var module in modules)
{
    module.AddModule(builder.Services, builder.Configuration);
}

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.AddDocument("v1", "API v1"));
}

app.MapHealthChecks("/health").AllowAnonymous();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

var apiV1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet)
    .RequireAuthorization()
    .RequireRateLimiting(RateLimiterPolicies.PerApiKey);

foreach (var module in modules)
{
    module.MapEndpoints(apiV1);
}

app.Run();

public partial class Program;
