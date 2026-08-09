namespace IdExtractor.Modules.Extraction.Contracts;

public sealed record MrzResultDto(IReadOnlyList<string> Raw, bool Valid);
