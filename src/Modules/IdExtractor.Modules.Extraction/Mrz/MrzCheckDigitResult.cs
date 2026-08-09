namespace IdExtractor.Modules.Extraction.Mrz;

/// <summary>
/// Per-field ICAO 9303 check-digit outcomes. <see cref="MrzResult.IsValid"/> is true
/// only when every applicable field here is true.
/// </summary>
public sealed record MrzCheckDigitResult(
    bool DocumentNumberValid,
    bool DateOfBirthValid,
    bool ExpiryDateValid,
    bool CompositeValid,
    bool? PersonalNumberValid);
