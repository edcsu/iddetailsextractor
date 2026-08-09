using IdExtractor.Modules.Extraction.Ocr;

namespace IdExtractor.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces the real OCR providers in integration tests — always returns a valid,
/// algorithmically-correct TD3 passport MRZ regardless of the uploaded image, so
/// the full pipeline (auth -> versioning -> MediatR -> MRZ parse -> field
/// extraction -> persistence) can be exercised without PaddleOCR or Azure Vision.
/// </summary>
public sealed class FakeOcrProvider : IOcrProvider
{
    public const string PassportLine1 = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<";
    public const string PassportLine2 = "L898902C36UTO7408122F1204159<<<<<<<<<<<<<<08";

    public string Name => "fake";

    public Task<IReadOnlyList<OcrLine>> RecognizeAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken)
    {
        IReadOnlyList<OcrLine> lines =
        [
            new OcrLine(PassportLine1, 0.99),
            new OcrLine(PassportLine2, 0.98)
        ];
        return Task.FromResult(lines);
    }
}
