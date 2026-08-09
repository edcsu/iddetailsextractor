using IdExtractor.Modules.Documents.Contracts;
using IdExtractor.Modules.Documents.Persistence;

namespace IdExtractor.Modules.Documents.Queries;

public static class ExtractionRecordMapper
{
    public static ExtractionRecordDto ToDto(ExtractionRecord record) => new(
        record.Id,
        record.DocumentType,
        record.DocumentTypeConfidence,
        record.Fields,
        record.Mrz,
        record.RawText,
        record.OcrProvider,
        record.CreatedAtUtc);
}
