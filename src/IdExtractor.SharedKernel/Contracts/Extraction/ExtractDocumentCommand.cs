using Ardalis.Result;
using MediatR;

namespace IdExtractor.SharedKernel.Contracts.Extraction;

/// <summary>
/// Runs the full extraction pipeline over an uploaded document: validate → OCR →
/// MRZ parse → field extraction → document-type detection. Handled by the
/// Extraction module. On success, also publishes <see cref="DocumentExtracted"/> so
/// the Documents module can persist the result and the source image.
/// </summary>
/// <param name="FileBytes">Raw upload bytes (image or PDF).</param>
/// <param name="ContentType">Client-supplied content type — a hint only; the actual type is detected from magic bytes.</param>
/// <param name="DocumentTypeHint">Optional caller hint (currently informational; detection still runs).</param>
public sealed record ExtractDocumentCommand(byte[] FileBytes, string ContentType, string? DocumentTypeHint = null)
    : IRequest<Result<ExtractionResultDto>>;
