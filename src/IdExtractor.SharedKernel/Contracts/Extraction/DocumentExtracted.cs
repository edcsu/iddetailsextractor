using MediatR;

namespace IdExtractor.SharedKernel.Contracts.Extraction;

/// <summary>
/// Raised by the Extraction module after a successful extraction. The Documents
/// module is the only consumer — it persists the <see cref="Result"/> and the
/// normalized source image (<see cref="ImageBytes"/>/<see cref="ImageContentType"/>)
/// for later retrieval and PDF report generation. This notification is the only
/// channel between the two modules.
/// </summary>
public sealed record DocumentExtracted(ExtractionResultDto Result, byte[] ImageBytes, string ImageContentType) : INotification;
