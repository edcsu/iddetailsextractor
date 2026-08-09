namespace IdExtractor.Modules.Extraction.Ocr;

/// <summary>Bound from the "Ocr" configuration section.</summary>
public sealed class OcrOptions
{
    public const string SectionName = "Ocr";

    /// <summary>Which <see cref="IOcrProvider"/> to use: "Paddle" (default) or "AzureVision".</summary>
    public string Provider { get; init; } = "Paddle";

    /// <summary>Base URL of the PaddleOCR sidecar, e.g. http://ocr-service:8001.</summary>
    public string PaddleBaseUrl { get; init; } = "http://localhost:8001";

    /// <summary>Azure AI Vision endpoint. Required only when Provider is "AzureVision".</summary>
    public string? AzureVisionEndpoint { get; init; }

    /// <summary>Azure AI Vision API key. Required only when Provider is "AzureVision".</summary>
    public string? AzureVisionApiKey { get; init; }
}
