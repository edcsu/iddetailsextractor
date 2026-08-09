using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace IdExtractor.Modules.Extraction.Ocr;

/// <summary>
/// Optional cloud OCR provider using Azure AI Vision's Read feature (free tier:
/// 5,000 calls/month). Selected via <c>Ocr:Provider = "AzureVision"</c>; requires
/// <c>Ocr:AzureVisionEndpoint</c> and <c>Ocr:AzureVisionApiKey</c> to be configured.
/// </summary>
public sealed class AzureVisionOcrProvider(HttpClient httpClient, IOptions<OcrOptions> options) : IOcrProvider
{
    public string Name => "azure-vision";

    public async Task<IReadOnlyList<OcrLine>> RecognizeAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.AzureVisionEndpoint) || string.IsNullOrWhiteSpace(config.AzureVisionApiKey))
        {
            throw new InvalidOperationException(
                "AzureVision OCR provider is selected but Ocr:AzureVisionEndpoint/AzureVisionApiKey are not configured.");
        }

        var requestUri = $"{config.AzureVisionEndpoint.TrimEnd('/')}/computervision/imageanalysis:analyze" +
                          "?api-version=2023-10-01&features=read";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Ocp-Apim-Subscription-Key", config.AzureVisionApiKey);
        request.Content = new ByteArrayContent(imageBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var lines = new List<OcrLine>();
        if (document.RootElement.TryGetProperty("readResult", out var readResult) &&
            readResult.TryGetProperty("blocks", out var blocks))
        {
            foreach (var block in blocks.EnumerateArray())
            {
                if (!block.TryGetProperty("lines", out var blockLines))
                {
                    continue;
                }

                foreach (var line in blockLines.EnumerateArray())
                {
                    lines.Add(new OcrLine(line.GetProperty("text").GetString() ?? "", AverageWordConfidence(line)));
                }
            }
        }

        return lines;
    }

    private static double AverageWordConfidence(JsonElement line)
    {
        if (!line.TryGetProperty("words", out var words))
        {
            return 1.0;
        }

        var confidences = words.EnumerateArray()
            .Where(word => word.TryGetProperty("confidence", out _))
            .Select(word => word.GetProperty("confidence").GetDouble())
            .ToList();

        return confidences.Count > 0 ? confidences.Average() : 1.0;
    }
}
