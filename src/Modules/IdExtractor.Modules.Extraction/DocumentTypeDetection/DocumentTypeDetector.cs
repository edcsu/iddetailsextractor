using IdExtractor.Modules.Extraction.Mrz;

namespace IdExtractor.Modules.Extraction.DocumentTypeDetection;

/// <summary>
/// Guesses the kind of document from an MRZ block when present (authoritative — the
/// document code tells us directly), otherwise falls back to English-language keyword
/// scoring over the raw OCR text. The keyword list is intentionally small and
/// English-first; extending it with more languages/synonyms is expected over time.
/// </summary>
public sealed class DocumentTypeDetector
{
    private static readonly (DocumentType Type, string[] Keywords)[] KeywordSets =
    [
        (DocumentType.Passport, ["PASSPORT", "PASSEPORT", "PASAPORTE", "REISEPASS"]),
        (DocumentType.DrivingLicence,
            ["DRIVING LICENCE", "DRIVER LICENSE", "DRIVER'S LICENSE", "PERMIS DE CONDUIRE", "FUHRERSCHEIN"]),
        (DocumentType.NationalId,
            ["IDENTITY CARD", "NATIONAL ID", "CARTE D'IDENTITE", "PERSONALAUSWEIS", "ID CARD"])
    ];

    public DocumentTypeDetectionResult Detect(MrzResult? mrz, string rawText) =>
        mrz is not null ? DetectFromMrz(mrz) : DetectFromKeywords(rawText);

    private static DocumentTypeDetectionResult DetectFromMrz(MrzResult mrz)
    {
        var documentType = mrz.Format == MrzFormat.Td3 || mrz.DocumentCode.StartsWith('P')
            ? DocumentType.Passport
            : DocumentType.NationalId;

        var confidence = mrz.IsValid ? 0.95 : 0.7;
        return new DocumentTypeDetectionResult(documentType, confidence);
    }

    private static DocumentTypeDetectionResult DetectFromKeywords(string rawText)
    {
        var normalized = rawText.ToUpperInvariant();

        var best = KeywordSets
            .Select(set => (set.Type, Score: set.Keywords.Count(keyword => normalized.Contains(keyword))))
            .OrderByDescending(result => result.Score)
            .First();

        return best.Score == 0
            ? new DocumentTypeDetectionResult(DocumentType.Unknown, 0)
            : new DocumentTypeDetectionResult(best.Type, Math.Min(0.3 + (0.2 * best.Score), 0.9));
    }
}
