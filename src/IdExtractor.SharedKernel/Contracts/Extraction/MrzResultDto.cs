namespace IdExtractor.SharedKernel.Contracts.Extraction;

public sealed record MrzResultDto(IReadOnlyList<string> Raw, bool Valid);
