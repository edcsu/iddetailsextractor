using Ardalis.Result;
using IdExtractor.Modules.Extraction.Contracts;
using IdExtractor.Modules.Extraction.DocumentTypeDetection;
using IdExtractor.Modules.Extraction.Fields;
using IdExtractor.Modules.Extraction.Mrz;
using IdExtractor.Modules.Extraction.Ocr;
using IdExtractor.Modules.Extraction.Validation;
using IdExtractor.SharedKernel.Telemetry;
using MediatR;

namespace IdExtractor.Modules.Extraction.Handlers;

public sealed class ExtractDocumentCommandHandler(
    IOcrProvider ocrProvider,
    MrzParser mrzParser,
    FieldExtractor fieldExtractor,
    DocumentTypeDetector documentTypeDetector,
    IPublisher publisher)
    : IRequestHandler<ExtractDocumentCommand, Result<ExtractionResultDto>>
{
    public async Task<Result<ExtractionResultDto>> Handle(ExtractDocumentCommand request, CancellationToken cancellationToken)
    {
        using var activity = IdExtractorActivitySource.Instance.StartActivity("ExtractDocument");

        var validated = UploadValidator.ValidateAndNormalize(request.FileBytes);
        if (!validated.IsSuccess)
        {
            return Result<ExtractionResultDto>.Invalid(validated.ValidationErrors.ToList());
        }

        var (imageBytes, contentType) = validated.Value;

        var ocrLines = await ocrProvider.RecognizeAsync(imageBytes, contentType, cancellationToken);
        var textLines = ocrLines.Select(line => line.Text).ToList();
        var rawText = string.Join('\n', textLines);

        var mrz = mrzParser.TryParse(textLines);
        var fields = fieldExtractor.Extract(textLines, mrz);
        var documentType = documentTypeDetector.Detect(mrz, rawText);

        var resultDto = new ExtractionResultDto(
            Id: Guid.CreateVersion7(),
            DocumentType: documentType.DocumentType,
            DocumentTypeConfidence: documentType.Confidence,
            Fields: fields.ToDictionary(kvp => kvp.Key, kvp => new ExtractedFieldDto(kvp.Value.Value, kvp.Value.Confidence)),
            Mrz: mrz is null ? null : new MrzResultDto(mrz.RawLines, mrz.IsValid),
            RawText: rawText,
            OcrProvider: ocrProvider.Name);

        await publisher.Publish(new DocumentExtracted(resultDto, imageBytes, contentType), cancellationToken);

        return Result.Success(resultDto);
    }
}
