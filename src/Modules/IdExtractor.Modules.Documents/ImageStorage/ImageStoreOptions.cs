namespace IdExtractor.Modules.Documents.ImageStorage;

public sealed class ImageStoreOptions
{
    public const string SectionName = "ImageStore";

    /// <summary>Directory source images are written to. Not served directly — never exposed over HTTP.</summary>
    public string RootPath { get; init; } = "./data/images";
}
