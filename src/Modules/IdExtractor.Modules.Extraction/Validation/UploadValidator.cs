using Ardalis.Result;
using SkiaSharp;

namespace IdExtractor.Modules.Extraction.Validation;

/// <summary>
/// Validates a raw upload (size, real file type via magic bytes rather than a
/// client-supplied content type) and normalizes it to a raster image the OCR
/// pipeline can consume — rendering page 1 of a PDF when that's what was uploaded.
/// </summary>
public static class UploadValidator
{
    public const int MaxUploadBytes = 10 * 1024 * 1024;

    public static Result<(byte[] ImageBytes, string ContentType)> ValidateAndNormalize(byte[] rawBytes)
    {
        if (rawBytes.Length == 0)
        {
            return Result.Invalid(new ValidationError("Uploaded file is empty."));
        }

        if (rawBytes.Length > MaxUploadBytes)
        {
            return Result.Invalid(new ValidationError($"Uploaded file exceeds the {MaxUploadBytes / (1024 * 1024)} MB limit."));
        }

        var detectedType = DetectContentType(rawBytes);
        if (detectedType is null)
        {
            return Result.Invalid(new ValidationError("Unsupported or unrecognized file type. Supported: JPEG, PNG, WebP, PDF."));
        }

        return detectedType == "application/pdf"
            ? RenderFirstPdfPage(rawBytes)
            : Result.Success((rawBytes, detectedType));
    }

    private static string? DetectContentType(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return "image/png";
        }

        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return "image/webp";
        }

        if (bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
        {
            return "application/pdf";
        }

        return null;
    }

    private static Result<(byte[], string)> RenderFirstPdfPage(byte[] pdfBytes)
    {
        try
        {
            using var pdfStream = new MemoryStream(pdfBytes);
            using var page = PDFtoImage.Conversion.ToImage(pdfStream, page: 0);
            using var outputStream = new MemoryStream();
            page.Encode(outputStream, SKEncodedImageFormat.Png, quality: 100);
            return Result.Success((outputStream.ToArray(), "image/png"));
        }
        catch (Exception ex)
        {
            return Result.Invalid(new ValidationError($"Could not render the PDF's first page: {ex.Message}"));
        }
    }
}
