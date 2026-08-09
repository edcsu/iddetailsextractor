using IdExtractor.SharedKernel.Contracts.Extraction;

namespace IdExtractor.Modules.Extraction.DocumentTypeDetection;

public sealed record DocumentTypeDetectionResult(DocumentType DocumentType, double Confidence);
