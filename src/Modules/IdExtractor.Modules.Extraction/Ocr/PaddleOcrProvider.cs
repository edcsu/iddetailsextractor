using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace IdExtractor.Modules.Extraction.Ocr;

/// <summary>Default OCR provider — calls the PaddleOCR FastAPI sidecar over HTTP.</summary>
public sealed class PaddleOcrProvider(HttpClient httpClient, IOptions<OcrOptions> options) : IOcrProvider
{
    public string Name => "paddle";

    public async Task<IReadOnlyList<OcrLine>> RecognizeAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        using var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(imageContent, "file", "document" + ExtensionFor(contentType));

        var requestUri = $"{options.Value.PaddleBaseUrl.TrimEnd('/')}/ocr";
        using var response = await httpClient.PostAsync(requestUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PaddleOcrResponse>(cancellationToken)
            ?? throw new InvalidOperationException("OCR sidecar returned an empty response.");

        return payload.Lines.Select(line => new OcrLine(line.Text, line.Confidence)).ToList();
    }

    private static string ExtensionFor(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg"
    };

    private sealed record PaddleOcrResponse([property: JsonPropertyName("lines")] IReadOnlyList<PaddleOcrLine> Lines);

    private sealed record PaddleOcrLine(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("confidence")] double Confidence);
}
