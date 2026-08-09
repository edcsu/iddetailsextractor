namespace IdExtractor.Modules.Extraction.Ocr;

/// <summary>One recognized line of text, as returned by whichever OCR provider ran.</summary>
public sealed record OcrLine(string Text, double Confidence);
