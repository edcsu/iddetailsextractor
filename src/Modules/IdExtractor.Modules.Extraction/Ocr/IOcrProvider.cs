namespace IdExtractor.Modules.Extraction.Ocr;

/// <summary>
/// Runs OCR over a document image. Implementations are swappable via
/// <see cref="OcrOptions.Provider"/> — the extraction pipeline only depends on this
/// interface, never on a specific engine.
/// </summary>
public interface IOcrProvider
{
    /// <summary>Identifies which provider actually ran, surfaced in the API response.</summary>
    string Name { get; }

    Task<IReadOnlyList<OcrLine>> RecognizeAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken);
}
