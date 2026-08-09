namespace IdExtractor.Modules.Extraction.Mrz;

/// <summary>A successfully structurally-parsed MRZ block. See <see cref="IsValid"/> for check-digit outcome.</summary>
public sealed record MrzResult(
    MrzFormat Format,
    string DocumentCode,
    string IssuingCountry,
    string Nationality,
    string DocumentNumber,
    string? PersonalNumber,
    string Surname,
    string GivenNames,
    DateOnly? DateOfBirth,
    DateOnly? ExpiryDate,
    string Sex,
    IReadOnlyList<string> RawLines,
    MrzCheckDigitResult CheckDigits,
    bool IsValid);
