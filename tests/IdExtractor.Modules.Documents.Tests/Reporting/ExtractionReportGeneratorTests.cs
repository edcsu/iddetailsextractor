using System.Text;
using IdExtractor.Modules.Documents.Persistence;
using IdExtractor.Modules.Documents.Reporting;
using IdExtractor.SharedKernel.Contracts.Extraction;
using SkiaSharp;

namespace IdExtractor.Modules.Documents.Tests.Reporting;

public class ExtractionReportGeneratorTests
{
    private readonly ExtractionReportGenerator _generator = new();

    private static readonly byte[] SampleImageBytes = CreateSamplePng();

    private static byte[] CreateSamplePng()
    {
        using var bitmap = new SKBitmap(20, 20);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void Generates_a_pdf_with_the_correct_file_signature()
    {
        var record = new ExtractionRecord(
            Guid.CreateVersion7(),
            DocumentType.Passport,
            0.95,
            new Dictionary<string, ExtractedFieldDto> { ["fullName"] = new("ANNA MARIA ERIKSSON", 0.95) },
            new MrzResultDto(["line1", "line2"], Valid: true),
            "raw ocr text",
            "paddle",
            "some-image.png",
            "image/png");

        var pdfBytes = _generator.Generate(record, SampleImageBytes);

        Assert.NotEmpty(pdfBytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }

    [Fact]
    public void Generates_a_pdf_when_there_is_no_mrz()
    {
        var record = new ExtractionRecord(
            Guid.CreateVersion7(),
            DocumentType.DrivingLicence,
            0.6,
            new Dictionary<string, ExtractedFieldDto> { ["documentNumber"] = new("DL1234567", 0.75) },
            mrz: null,
            "raw ocr text",
            "paddle",
            "some-image.png",
            "image/png");

        var pdfBytes = _generator.Generate(record, SampleImageBytes);

        Assert.NotEmpty(pdfBytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }

    [Fact]
    public void Generates_a_pdf_when_there_are_no_fields()
    {
        var record = new ExtractionRecord(
            Guid.CreateVersion7(),
            DocumentType.Unknown,
            0,
            new Dictionary<string, ExtractedFieldDto>(),
            mrz: null,
            "",
            "paddle",
            "some-image.png",
            "image/png");

        var pdfBytes = _generator.Generate(record, SampleImageBytes);

        Assert.NotEmpty(pdfBytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }
}
