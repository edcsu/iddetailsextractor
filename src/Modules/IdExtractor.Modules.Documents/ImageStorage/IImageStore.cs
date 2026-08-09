namespace IdExtractor.Modules.Documents.ImageStorage;

/// <summary>
/// Persists and retrieves the source image behind an extraction. Images are stored
/// outside the database and never inlined into JSON API responses — only used
/// internally when rendering a PDF report.
/// </summary>
public interface IImageStore
{
    /// <summary>Saves the image and returns a path relative to the store's root.</summary>
    Task<string> SaveAsync(Guid extractionId, byte[] imageBytes, string contentType, CancellationToken cancellationToken);

    Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken);
}
