namespace IdExtractor.Modules.Documents.Contracts;

public sealed record PagedList<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);
