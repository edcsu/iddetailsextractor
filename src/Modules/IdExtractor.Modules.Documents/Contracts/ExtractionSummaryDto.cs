using IdExtractor.SharedKernel.Contracts.Extraction;

namespace IdExtractor.Modules.Documents.Contracts;

/// <summary>List-view row — metadata only, no raw text or field values.</summary>
public sealed record ExtractionSummaryDto(Guid Id, DocumentType DocumentType, double DocumentTypeConfidence, DateTimeOffset CreatedAtUtc);
