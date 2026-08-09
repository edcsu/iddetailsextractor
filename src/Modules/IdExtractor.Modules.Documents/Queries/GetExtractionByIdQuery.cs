using Ardalis.Result;
using IdExtractor.Modules.Documents.Contracts;
using IdExtractor.Modules.Documents.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdExtractor.Modules.Documents.Queries;

public sealed record GetExtractionByIdQuery(Guid Id) : IRequest<Result<ExtractionRecordDto>>;

public sealed class GetExtractionByIdQueryHandler(DocumentsDbContext db) : IRequestHandler<GetExtractionByIdQuery, Result<ExtractionRecordDto>>
{
    public async Task<Result<ExtractionRecordDto>> Handle(GetExtractionByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await db.ExtractionRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        return record is null
            ? Result<ExtractionRecordDto>.NotFound()
            : Result.Success(ExtractionRecordMapper.ToDto(record));
    }
}
