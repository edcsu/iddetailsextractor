using Ardalis.Result.AspNetCore;
using IdExtractor.Modules.Documents.ImageStorage;
using IdExtractor.Modules.Documents.Persistence;
using IdExtractor.Modules.Documents.Queries;
using IdExtractor.Modules.Documents.Reporting;
using IdExtractor.SharedKernel.Auditing;
using IdExtractor.SharedKernel.Modules;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdExtractor.Modules.Documents;

public sealed class DocumentsModule : IModule
{
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ImageStoreOptions>(configuration.GetSection(ImageStoreOptions.SectionName));
        services.AddScoped<IImageStore, FileSystemImageStore>();
        services.AddSingleton<ExtractionReportGenerator>();

        services.AddDbContext<DocumentsDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DocumentsDb"));
            options.AddInterceptors(new AuditSaveChangesInterceptor(sp.GetRequiredService<ICurrentActor>()));
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/extractions/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetExtractionByIdQuery(id), cancellationToken);
                return result.ToMinimalApiResult();
            })
            .WithName("GetExtractionById");

        endpoints.MapGet("/extractions", async (int? pageNumber, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ListExtractionsQuery(pageNumber ?? 1, pageSize ?? 20), cancellationToken);
                return result.ToMinimalApiResult();
            })
            .WithName("ListExtractions");

        endpoints.MapGet("/extractions/{id:guid}/report.pdf", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GenerateExtractionReportQuery(id), cancellationToken);
                return result.IsSuccess
                    ? Results.File(result.Value, "application/pdf", $"{id}-report.pdf")
                    : result.ToMinimalApiResult();
            })
            .WithName("GetExtractionReport");
    }
}
