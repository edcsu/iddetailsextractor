using System.Text.RegularExpressions;
using IdExtractor.Modules.Extraction.Mrz;

namespace IdExtractor.Modules.Extraction.Fields;

/// <summary>
/// Builds the structured field map for an extraction result. When an MRZ was parsed,
/// its fields are authoritative and seed the map first; label-based heuristics over
/// the raw OCR text then fill in whatever the MRZ didn't cover (or, for documents
/// without an MRZ — most driving licences — provide the only source of fields).
/// </summary>
public sealed class FieldExtractor
{
    private static readonly (string Field, string[] Labels)[] LabelMap =
    [
        (FieldNames.DateOfBirth, ["DATE OF BIRTH", "DOB", "BIRTH DATE", "GEBURTSDATUM"]),
        (FieldNames.ExpiryDate, ["DATE OF EXPIRY", "EXPIRY DATE", "EXPIRES", "VALID UNTIL", "DATE D'EXPIRATION"]),
        (FieldNames.DocumentNumber,
            ["DOCUMENT NO", "DOCUMENT NUMBER", "ID NO", "LICENCE NO", "LICENSE NO", "PASSPORT NO", "CARD NUMBER"]),
        (FieldNames.Nationality, ["NATIONALITY", "NATIONALITE"]),
        (FieldNames.Sex, ["SEX", "GENDER", "SEXE"]),
        (FieldNames.FullName, ["FULL NAME", "SURNAME", "NAME"])
    ];

    private static readonly Regex DatePattern = new(
        @"\b\d{1,2}[/.\-]\d{1,2}[/.\-]\d{2,4}\b|\b\d{4}[/.\-]\d{1,2}[/.\-]\d{1,2}\b",
        RegexOptions.Compiled);

    public IReadOnlyDictionary<string, ExtractedField> Extract(IReadOnlyList<string> ocrLines, MrzResult? mrz)
    {
        var fields = new Dictionary<string, ExtractedField>();

        if (mrz is not null)
        {
            SeedFromMrz(fields, mrz);
        }

        ExtractFromLabels(fields, ocrLines);

        return fields;
    }

    private static void SeedFromMrz(Dictionary<string, ExtractedField> fields, MrzResult mrz)
    {
        var confidence = mrz.IsValid ? 0.95 : 0.75;

        var fullName = string.Join(' ', new[] { mrz.GivenNames, mrz.Surname }.Where(p => !string.IsNullOrWhiteSpace(p)));
        SetIfPresent(fields, FieldNames.FullName, fullName, confidence);
        SetIfPresent(fields, FieldNames.DocumentNumber, mrz.DocumentNumber, confidence);
        SetIfPresent(fields, FieldNames.Nationality, mrz.Nationality, confidence);
        SetIfPresent(fields, FieldNames.IssuingCountry, mrz.IssuingCountry, confidence);
        SetIfPresent(fields, FieldNames.Sex, mrz.Sex, confidence);

        if (mrz.DateOfBirth is { } dob)
        {
            fields[FieldNames.DateOfBirth] = new ExtractedField(dob.ToString("yyyy-MM-dd"), confidence);
        }

        if (mrz.ExpiryDate is { } expiry)
        {
            fields[FieldNames.ExpiryDate] = new ExtractedField(expiry.ToString("yyyy-MM-dd"), confidence);
        }
    }

    private static void SetIfPresent(Dictionary<string, ExtractedField> fields, string field, string? value, double confidence)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields[field] = new ExtractedField(value, confidence);
        }
    }

    private static void ExtractFromLabels(Dictionary<string, ExtractedField> fields, IReadOnlyList<string> ocrLines)
    {
        for (var i = 0; i < ocrLines.Count; i++)
        {
            var line = ocrLines[i];
            var upperLine = line.ToUpperInvariant();

            foreach (var (field, labels) in LabelMap)
            {
                if (fields.ContainsKey(field))
                {
                    continue; // MRZ (or an earlier, more specific label match) already won this field.
                }

                var matchedLabel = labels.FirstOrDefault(label => upperLine.Contains(label));
                if (matchedLabel is null)
                {
                    continue;
                }

                var remainder = StripLabel(line, matchedLabel);
                if (remainder.Length > 0 && ExtractValue(field, remainder) is { } sameLineValue)
                {
                    fields[field] = new ExtractedField(sameLineValue, 0.75);
                    continue;
                }

                var nextLine = i + 1 < ocrLines.Count ? ocrLines[i + 1].Trim() : null;
                if (!string.IsNullOrWhiteSpace(nextLine))
                {
                    var value = ExtractValue(field, nextLine) ?? nextLine;
                    fields[field] = new ExtractedField(value, 0.6);
                }
            }
        }
    }

    private static string StripLabel(string line, string label)
    {
        var index = line.ToUpperInvariant().IndexOf(label, StringComparison.Ordinal);
        return index < 0 ? "" : line[(index + label.Length)..].TrimStart(':', '-', ' ', '.').Trim();
    }

    private static string? ExtractValue(string field, string text)
    {
        if (field is not (FieldNames.DateOfBirth or FieldNames.ExpiryDate))
        {
            return text.Length > 0 ? text : null;
        }

        var match = DatePattern.Match(text);
        return match.Success ? NormalizeDate(match.Value) : null;
    }

    private static string NormalizeDate(string raw)
    {
        var parts = raw.Split(['/', '.', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return raw;
        }

        int year, month, day;
        if (parts[0].Length == 4)
        {
            // Already year-first (YYYY-MM-DD or similar).
            year = int.Parse(parts[0]);
            month = int.Parse(parts[1]);
            day = int.Parse(parts[2]);
        }
        else
        {
            // Day-first (DD-MM-YYYY) is the more common convention outside the US.
            day = int.Parse(parts[0]);
            month = int.Parse(parts[1]);
            year = int.Parse(parts[2]);
            if (year < 100)
            {
                year += year > DateTime.UtcNow.Year % 100 ? 1900 : 2000;
            }
        }

        try
        {
            return new DateOnly(year, month, day).ToString("yyyy-MM-dd");
        }
        catch (ArgumentOutOfRangeException)
        {
            return raw;
        }
    }
}
