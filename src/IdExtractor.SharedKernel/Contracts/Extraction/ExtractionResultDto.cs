namespace IdExtractor.SharedKernel.Contracts.Extraction;

/// <summary>
/// The outcome of one extraction. <see cref="Id"/> is minted by the extraction
/// pipeline itself (GUIDv7) so it's available before persistence — the Documents
/// module reuses the same id when it saves the record, so the API response id and
/// the stored record id always match.
/// </summary>
public sealed record ExtractionResultDto(
    Guid Id,
    DocumentType DocumentType,
    double DocumentTypeConfidence,
    IReadOnlyDictionary<string, ExtractedFieldDto> Fields,
    MrzResultDto? Mrz,
    string RawText,
    string OcrProvider);
