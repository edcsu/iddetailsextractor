using Ardalis.Result.AspNetCore;
using IdExtractor.Modules.Extraction.DocumentTypeDetection;
using IdExtractor.Modules.Extraction.Fields;
using IdExtractor.Modules.Extraction.Mrz;
using IdExtractor.Modules.Extraction.Ocr;
using IdExtractor.Modules.Extraction.Validation;
using IdExtractor.SharedKernel.Contracts.Extraction;
using IdExtractor.SharedKernel.Modules;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IdExtractor.Modules.Extraction;

public sealed class ExtractionModule : IModule
{
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OcrOptions>(configuration.GetSection(OcrOptions.SectionName));

        services.AddHttpClient<PaddleOcrProvider>();
        services.AddHttpClient<AzureVisionOcrProvider>();
        services.AddScoped<IOcrProvider>(sp =>
        {
            var provider = sp.GetRequiredService<IOptions<OcrOptions>>().Value.Provider;
            return provider.Equals("AzureVision", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<AzureVisionOcrProvider>()
                : sp.GetRequiredService<PaddleOcrProvider>();
        });

        services.AddSingleton<MrzParser>();
        services.AddSingleton<FieldExtractor>();
        services.AddSingleton<DocumentTypeDetector>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/extractions", async (
                IFormFile file,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await using var stream = file.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);

                var result = await sender.Send(
                    new ExtractDocumentCommand(buffer.ToArray(), file.ContentType),
                    cancellationToken);

                return result.ToMinimalApiResult();
            })
            .DisableAntiforgery()
            .WithName("ExtractDocument")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ExtractionResultDto>()
            .ProducesValidationProblem();
    }
}
