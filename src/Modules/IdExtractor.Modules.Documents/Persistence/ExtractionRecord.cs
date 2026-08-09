using IdExtractor.SharedKernel.Auditing;
using IdExtractor.SharedKernel.Contracts.Extraction;
using IdExtractor.SharedKernel.Entities;

namespace IdExtractor.Modules.Documents.Persistence;

/// <summary>
/// The persisted outcome of one extraction, plus a pointer to its saved source
/// image. <see cref="Entity.Id"/> is set to the same GUIDv7 the Extraction module
/// minted for the <c>ExtractionResultDto</c>, so the API response id and the stored
/// record id always match. <see cref="Fields"/>, <see cref="Mrz"/> and
/// <see cref="RawText"/> can carry extracted PII, so they're marked
/// <see cref="SensitiveAttribute"/> — kept out of the audit log payload.
/// </summary>
public sealed class ExtractionRecord : AuditableEntity
{
    private ExtractionRecord()
    {
        // EF Core materialization.
    }

    public ExtractionRecord(
        Guid id,
        DocumentType documentType,
        double documentTypeConfidence,
        IReadOnlyDictionary<string, ExtractedFieldDto> fields,
        MrzResultDto? mrz,
        string rawText,
        string ocrProvider,
        string imagePath,
        string imageContentType)
    {
        Id = id;
        DocumentType = documentType;
        DocumentTypeConfidence = documentTypeConfidence;
        Fields = fields;
        Mrz = mrz;
        RawText = rawText;
        OcrProvider = ocrProvider;
        ImagePath = imagePath;
        ImageContentType = imageContentType;
    }

    public DocumentType DocumentType { get; private set; }
    public double DocumentTypeConfidence { get; private set; }

    [Sensitive]
    public IReadOnlyDictionary<string, ExtractedFieldDto> Fields { get; private set; } = new Dictionary<string, ExtractedFieldDto>();

    [Sensitive]
    public MrzResultDto? Mrz { get; private set; }

    [Sensitive]
    public string RawText { get; private set; } = "";

    public string OcrProvider { get; private set; } = "";
    public string ImagePath { get; private set; } = "";
    public string ImageContentType { get; private set; } = "";
}
