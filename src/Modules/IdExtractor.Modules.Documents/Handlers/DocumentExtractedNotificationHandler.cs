using IdExtractor.Modules.Documents.ImageStorage;
using IdExtractor.Modules.Documents.Persistence;
using IdExtractor.SharedKernel.Contracts.Extraction;
using MediatR;

namespace IdExtractor.Modules.Documents.Handlers;

/// <summary>
/// The only place the Documents module reacts to the Extraction module — persists
/// the extraction result and its source image. This notification is the sole
/// integration point between the two modules.
/// </summary>
public sealed class DocumentExtractedNotificationHandler(DocumentsDbContext db, IImageStore imageStore)
    : INotificationHandler<DocumentExtracted>
{
    public async Task Handle(DocumentExtracted notification, CancellationToken cancellationToken)
    {
        var imagePath = await imageStore.SaveAsync(
            notification.Result.Id, notification.ImageBytes, notification.ImageContentType, cancellationToken);

        var record = new ExtractionRecord(
            notification.Result.Id,
            notification.Result.DocumentType,
            notification.Result.DocumentTypeConfidence,
            notification.Result.Fields,
            notification.Result.Mrz,
            notification.Result.RawText,
            notification.Result.OcrProvider,
            imagePath,
            notification.ImageContentType);

        db.ExtractionRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
