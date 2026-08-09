using Ardalis.Result;
using IdExtractor.Modules.Documents.ImageStorage;
using IdExtractor.Modules.Documents.Persistence;
using IdExtractor.Modules.Documents.Reporting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdExtractor.Modules.Documents.Queries;

public sealed record GenerateExtractionReportQuery(Guid Id) : IRequest<Result<byte[]>>;

public sealed class GenerateExtractionReportQueryHandler(
    DocumentsDbContext db,
    IImageStore imageStore,
    ExtractionReportGenerator reportGenerator)
    : IRequestHandler<GenerateExtractionReportQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GenerateExtractionReportQuery request, CancellationToken cancellationToken)
    {
        var record = await db.ExtractionRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (record is null)
        {
            return Result<byte[]>.NotFound();
        }

        var imageBytes = await imageStore.ReadAsync(record.ImagePath, cancellationToken);
        if (imageBytes is null)
        {
            return Result<byte[]>.NotFound("The source image for this extraction is missing.");
        }

        var pdfBytes = reportGenerator.Generate(record, imageBytes);
        return Result.Success(pdfBytes);
    }
}
