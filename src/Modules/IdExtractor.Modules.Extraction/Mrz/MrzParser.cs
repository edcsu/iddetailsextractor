namespace IdExtractor.Modules.Extraction.Mrz;

/// <summary>
/// Detects and parses ICAO 9303 machine-readable zones (TD1/TD2/TD3) out of raw OCR
/// text lines, validating each field's check digit per the standard algorithm. Lines
/// are matched by exact expected length after normalization — OCR noise that shifts
/// line length (missed/extra characters) will fail to match, which is an accepted
/// limitation for this first version.
/// </summary>
public sealed class MrzParser
{
    public MrzResult? TryParse(IEnumerable<string> ocrLines)
    {
        var normalized = ocrLines
            .Select(Normalize)
            .Where(line => line.Length > 0)
            .ToList();

        return TryParseTd3(normalized) ?? TryParseTd2(normalized) ?? TryParseTd1(normalized);
    }

    private static string Normalize(string line) =>
        new(line.Where(c => !char.IsWhiteSpace(c)).Select(char.ToUpperInvariant).ToArray());

    private static MrzResult? TryParseTd3(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count - 1; i++)
        {
            var line1 = lines[i];
            var line2 = lines[i + 1];
            if (line1.Length != 44 || line2.Length != 44)
            {
                continue;
            }

            var docCode = TrimFiller(line1[..2]);
            var issuingCountry = TrimFiller(line1[2..5]);
            var (surname, givenNames) = ParseNameField(line1[5..]);

            var documentNumber = line2[..9];
            var documentNumberCheck = line2[9];
            var nationality = TrimFiller(line2[10..13]);
            var dobRaw = line2[13..19];
            var dobCheck = line2[19];
            var sex = line2[20..21];
            var expiryRaw = line2[21..27];
            var expiryCheck = line2[27];
            var personalNumberRaw = line2[28..42];
            var personalNumberCheck = line2[42];
            var compositeCheck = line2[43];

            var documentNumberValid = CheckDigitMatches(documentNumber, documentNumberCheck);
            var dobValid = CheckDigitMatches(dobRaw, dobCheck);
            var expiryValid = CheckDigitMatches(expiryRaw, expiryCheck);
            var personalNumberValid = CheckDigitMatches(personalNumberRaw, personalNumberCheck);

            var compositeData = line2[..10] + line2[13..20] + line2[21..43];
            var compositeValid = CheckDigitMatches(compositeData, compositeCheck);

            var checkDigits = new MrzCheckDigitResult(documentNumberValid, dobValid, expiryValid, compositeValid, personalNumberValid);

            return new MrzResult(
                MrzFormat.Td3,
                docCode,
                issuingCountry,
                nationality,
                TrimFiller(documentNumber),
                NullIfEmpty(TrimFiller(personalNumberRaw)),
                surname,
                givenNames,
                ParseMrzDate(dobRaw, isBirthDate: true),
                ParseMrzDate(expiryRaw, isBirthDate: false),
                sex,
                [line1, line2],
                checkDigits,
                documentNumberValid && dobValid && expiryValid && compositeValid);
        }

        return null;
    }

    private static MrzResult? TryParseTd2(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count - 1; i++)
        {
            var line1 = lines[i];
            var line2 = lines[i + 1];
            if (line1.Length != 36 || line2.Length != 36)
            {
                continue;
            }

            var docCode = TrimFiller(line1[..2]);
            var issuingCountry = TrimFiller(line1[2..5]);
            var (surname, givenNames) = ParseNameField(line1[5..]);

            var documentNumber = line2[..9];
            var documentNumberCheck = line2[9];
            var nationality = TrimFiller(line2[10..13]);
            var dobRaw = line2[13..19];
            var dobCheck = line2[19];
            var sex = line2[20..21];
            var expiryRaw = line2[21..27];
            var expiryCheck = line2[27];
            var optional = line2[28..35];
            var compositeCheck = line2[35];

            var documentNumberValid = CheckDigitMatches(documentNumber, documentNumberCheck);
            var dobValid = CheckDigitMatches(dobRaw, dobCheck);
            var expiryValid = CheckDigitMatches(expiryRaw, expiryCheck);

            var compositeData = line2[..10] + line2[13..20] + line2[21..35];
            var compositeValid = CheckDigitMatches(compositeData, compositeCheck);

            var checkDigits = new MrzCheckDigitResult(documentNumberValid, dobValid, expiryValid, compositeValid, PersonalNumberValid: null);

            return new MrzResult(
                MrzFormat.Td2,
                docCode,
                issuingCountry,
                nationality,
                TrimFiller(documentNumber),
                NullIfEmpty(TrimFiller(optional)),
                surname,
                givenNames,
                ParseMrzDate(dobRaw, isBirthDate: true),
                ParseMrzDate(expiryRaw, isBirthDate: false),
                sex,
                [line1, line2],
                checkDigits,
                documentNumberValid && dobValid && expiryValid && compositeValid);
        }

        return null;
    }

    private static MrzResult? TryParseTd1(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count - 2; i++)
        {
            var line1 = lines[i];
            var line2 = lines[i + 1];
            var line3 = lines[i + 2];
            if (line1.Length != 30 || line2.Length != 30 || line3.Length != 30)
            {
                continue;
            }

            var docCode = TrimFiller(line1[..2]);
            var issuingCountry = TrimFiller(line1[2..5]);
            var documentNumber = line1[5..14];
            var documentNumberCheck = line1[14];
            var optional1 = line1[15..30];

            var dobRaw = line2[..6];
            var dobCheck = line2[6];
            var sex = line2[7..8];
            var expiryRaw = line2[8..14];
            var expiryCheck = line2[14];
            var nationality = TrimFiller(line2[15..18]);
            var compositeCheck = line2[29];

            var (surname, givenNames) = ParseNameField(line3);

            var documentNumberValid = CheckDigitMatches(documentNumber, documentNumberCheck);
            var dobValid = CheckDigitMatches(dobRaw, dobCheck);
            var expiryValid = CheckDigitMatches(expiryRaw, expiryCheck);

            var compositeData = documentNumber + documentNumberCheck + optional1
                                 + line2[..7] + line2[8..15] + line2[18..29];
            var compositeValid = CheckDigitMatches(compositeData, compositeCheck);

            var checkDigits = new MrzCheckDigitResult(documentNumberValid, dobValid, expiryValid, compositeValid, PersonalNumberValid: null);

            return new MrzResult(
                MrzFormat.Td1,
                docCode,
                issuingCountry,
                nationality,
                TrimFiller(documentNumber),
                PersonalNumber: null,
                surname,
                givenNames,
                ParseMrzDate(dobRaw, isBirthDate: true),
                ParseMrzDate(expiryRaw, isBirthDate: false),
                sex,
                [line1, line2, line3],
                checkDigits,
                documentNumberValid && dobValid && expiryValid && compositeValid);
        }

        return null;
    }

    private static (string Surname, string GivenNames) ParseNameField(string nameField)
    {
        var parts = nameField.Split("<<", 2);
        var surname = CleanNamePart(parts[0]);
        var givenNames = parts.Length > 1 ? CleanNamePart(parts[1]) : "";
        return (surname, givenNames);
    }

    private static string CleanNamePart(string part) =>
        string.Join(' ', part.Split('<', StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string TrimFiller(string field) => field.TrimEnd('<');

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    private static DateOnly? ParseMrzDate(string yymmdd, bool isBirthDate)
    {
        if (yymmdd.Length != 6 || !yymmdd.All(char.IsAsciiDigit))
        {
            return null;
        }

        var yy = int.Parse(yymmdd[..2]);
        var month = int.Parse(yymmdd[2..4]);
        var day = int.Parse(yymmdd[4..6]);

        // Birth dates: a two-digit year later than "now" almost always means last
        // century (nobody's born in the future). Expiry dates: MRZ-bearing documents
        // are a 2000s-and-later format, so 2000+YY is the practical assumption.
        var century = isBirthDate
            ? yy > DateTime.UtcNow.Year % 100 ? 1900 : 2000
            : 2000;

        try
        {
            return new DateOnly(century + yy, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static bool CheckDigitMatches(string data, char expectedCheckDigit)
    {
        // Some issuers fill an unused check digit with '<' instead of computing it
        // (most commonly the passport personal-number check digit when that field is
        // blank). Treat that as "not applicable" rather than a failure.
        if (expectedCheckDigit == '<')
        {
            return true;
        }

        if (!char.IsAsciiDigit(expectedCheckDigit))
        {
            return false;
        }

        return ComputeCheckDigit(data) == expectedCheckDigit - '0';
    }

    private static int ComputeCheckDigit(string data)
    {
        ReadOnlySpan<int> weights = [7, 3, 1];
        var sum = 0;
        for (var i = 0; i < data.Length; i++)
        {
            sum += CharValue(data[i]) * weights[i % 3];
        }

        return sum % 10;
    }

    private static int CharValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'A' and <= 'Z' => c - 'A' + 10,
        '<' => 0,
        _ => 0
    };
}
