using Microsoft.Extensions.Options;

namespace IdExtractor.Modules.Documents.ImageStorage;

public sealed class FileSystemImageStore(IOptions<ImageStoreOptions> options) : IImageStore
{
    public async Task<string> SaveAsync(Guid extractionId, byte[] imageBytes, string contentType, CancellationToken cancellationToken)
    {
        var root = options.Value.RootPath;
        Directory.CreateDirectory(root);

        var relativePath = $"{extractionId}.{ExtensionFor(contentType)}";
        var fullPath = Path.Combine(root, relativePath);

        await File.WriteAllBytesAsync(fullPath, imageBytes, cancellationToken);

        return relativePath;
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(options.Value.RootPath, relativePath);
        return File.Exists(fullPath) ? await File.ReadAllBytesAsync(fullPath, cancellationToken) : null;
    }

    private static string ExtensionFor(string contentType) => contentType switch
    {
        "image/png" => "png",
        "image/webp" => "webp",
        _ => "jpg"
    };
}
