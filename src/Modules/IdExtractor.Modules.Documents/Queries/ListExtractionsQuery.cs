using Ardalis.Result;
using IdExtractor.Modules.Documents.Contracts;
using IdExtractor.Modules.Documents.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdExtractor.Modules.Documents.Queries;

public sealed record ListExtractionsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<Result<PagedList<ExtractionSummaryDto>>>;

public sealed class ListExtractionsQueryHandler(DocumentsDbContext db)
    : IRequestHandler<ListExtractionsQuery, Result<PagedList<ExtractionSummaryDto>>>
{
    public async Task<Result<PagedList<ExtractionSummaryDto>>> Handle(ListExtractionsQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.ExtractionRecords.AsNoTracking().OrderByDescending(r => r.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ExtractionSummaryDto(r.Id, r.DocumentType, r.DocumentTypeConfidence, r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedList<ExtractionSummaryDto>(items, pageNumber, pageSize, totalCount));
    }
}
