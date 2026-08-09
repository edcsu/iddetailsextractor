using IdExtractor.SharedKernel.Contracts.Extraction;

namespace IdExtractor.Modules.Documents.Contracts;

/// <summary>Full stored extraction result, as returned by <c>GET /extractions/{id}</c>.</summary>
public sealed record ExtractionRecordDto(
    Guid Id,
    DocumentType DocumentType,
    double DocumentTypeConfidence,
    IReadOnlyDictionary<string, ExtractedFieldDto> Fields,
    MrzResultDto? Mrz,
    string RawText,
    string OcrProvider,
    DateTimeOffset CreatedAtUtc);
