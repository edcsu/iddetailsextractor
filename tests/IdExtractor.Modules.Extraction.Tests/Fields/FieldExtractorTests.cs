using IdExtractor.Modules.Extraction.Fields;
using IdExtractor.Modules.Extraction.Mrz;

namespace IdExtractor.Modules.Extraction.Tests.Fields;

public class FieldExtractorTests
{
    private readonly FieldExtractor _extractor = new();

    [Fact]
    public void Seeds_fields_from_a_valid_mrz_with_high_confidence()
    {
        var mrz = new MrzResult(
            MrzFormat.Td3, "P", "UTO", "UTO", "L898902C3", null, "ERIKSSON", "ANNA MARIA",
            new DateOnly(1974, 8, 12), new DateOnly(2012, 4, 15), "F", ["l1", "l2"],
            new MrzCheckDigitResult(true, true, true, true, true), IsValid: true);

        var fields = _extractor.Extract(ocrLines: [], mrz);

        Assert.Equal("ANNA MARIA ERIKSSON", fields[FieldNames.FullName].Value);
        Assert.Equal("L898902C3", fields[FieldNames.DocumentNumber].Value);
        Assert.Equal("1974-08-12", fields[FieldNames.DateOfBirth].Value);
        Assert.Equal("2012-04-15", fields[FieldNames.ExpiryDate].Value);
        Assert.Equal("UTO", fields[FieldNames.Nationality].Value);
        Assert.Equal("F", fields[FieldNames.Sex].Value);
        Assert.True(fields[FieldNames.FullName].Confidence >= 0.9);
    }

    [Fact]
    public void Mrz_fields_take_precedence_over_conflicting_label_matches()
    {
        var mrz = new MrzResult(
            MrzFormat.Td3, "P", "UTO", "UTO", "L898902C3", null, "ERIKSSON", "ANNA MARIA",
            new DateOnly(1974, 8, 12), new DateOnly(2012, 4, 15), "F", ["l1", "l2"],
            new MrzCheckDigitResult(true, true, true, true, true), IsValid: true);

        var fields = _extractor.Extract(["Document Number: X999999"], mrz);

        Assert.Equal("L898902C3", fields[FieldNames.DocumentNumber].Value);
    }

    [Fact]
    public void Extracts_a_label_value_on_the_same_line()
    {
        var fields = _extractor.Extract(["Licence No: DL1234567"], mrz: null);

        Assert.Equal("DL1234567", fields[FieldNames.DocumentNumber].Value);
        Assert.Equal(0.75, fields[FieldNames.DocumentNumber].Confidence);
    }

    [Fact]
    public void Falls_back_to_the_next_line_when_the_label_line_has_no_value()
    {
        var fields = _extractor.Extract(["Nationality", "CANADIAN"], mrz: null);

        Assert.Equal("CANADIAN", fields[FieldNames.Nationality].Value);
        Assert.Equal(0.6, fields[FieldNames.Nationality].Confidence);
    }

    [Theory]
    [InlineData("Date of Birth: 12/08/1974", "1974-08-12")]
    [InlineData("DOB 1974-08-12", "1974-08-12")]
    public void Normalizes_dates_to_iso_8601(string line, string expectedIso)
    {
        var fields = _extractor.Extract([line], mrz: null);

        Assert.Equal(expectedIso, fields[FieldNames.DateOfBirth].Value);
    }

    [Fact]
    public void Returns_no_fields_when_nothing_matches()
    {
        var fields = _extractor.Extract(["random unrelated text"], mrz: null);

        Assert.Empty(fields);
    }
}
