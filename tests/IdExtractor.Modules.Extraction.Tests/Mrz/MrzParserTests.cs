using IdExtractor.Modules.Extraction.Mrz;

namespace IdExtractor.Modules.Extraction.Tests.Mrz;

public class MrzParserTests
{
    private readonly MrzParser _parser = new();

    // Every fixture below is algorithmically generated per the ICAO 9303 field
    // layout and check-digit algorithm (modulus 10, weights 7/3/1) — not copied from
    // a remembered "famous" sample — so the expected check digits are known-correct.

    private const string Td3Line1 = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<";
    private const string Td3Line2 = "L898902C36UTO7408122F1204159<<<<<<<<<<<<<<08";

    private const string Td2Line1 = "I<UTOMUSTERMANN<<ERIKA<<<<<<<<<<<<<<";
    private const string Td2Line2 = "D231458907UTO7408122F1204159<<<<<<<6";

    private const string Td1Line1 = "IDUTOD231458907<<<<<<<<<<<<<<<";
    private const string Td1Line2 = "7408122F1204159UTO<<<<<<<<<<<6";
    private const string Td1Line3 = "MUSTERMANN<<ERIKA<<<<<<<<<<<<<";

    [Fact]
    public void Parses_a_valid_td3_passport_mrz()
    {
        var result = _parser.TryParse([Td3Line1, Td3Line2]);

        Assert.NotNull(result);
        Assert.Equal(MrzFormat.Td3, result.Format);
        Assert.Equal("UTO", result.IssuingCountry);
        Assert.Equal("UTO", result.Nationality);
        Assert.Equal("ERIKSSON", result.Surname);
        Assert.Equal("ANNA MARIA", result.GivenNames);
        Assert.Equal("L898902C3", result.DocumentNumber);
        Assert.Equal("F", result.Sex);
        Assert.Equal(new DateOnly(1974, 8, 12), result.DateOfBirth);
        Assert.Equal(new DateOnly(2012, 4, 15), result.ExpiryDate);
        Assert.Null(result.PersonalNumber);
        Assert.True(result.IsValid);
        Assert.True(result.CheckDigits.DocumentNumberValid);
        Assert.True(result.CheckDigits.DateOfBirthValid);
        Assert.True(result.CheckDigits.ExpiryDateValid);
        Assert.True(result.CheckDigits.CompositeValid);
        Assert.True(result.CheckDigits.PersonalNumberValid);
    }

    [Fact]
    public void Parses_a_valid_td2_id_card_mrz()
    {
        var result = _parser.TryParse([Td2Line1, Td2Line2]);

        Assert.NotNull(result);
        Assert.Equal(MrzFormat.Td2, result.Format);
        Assert.Equal("UTO", result.IssuingCountry);
        Assert.Equal("MUSTERMANN", result.Surname);
        Assert.Equal("ERIKA", result.GivenNames);
        Assert.Equal("D23145890", result.DocumentNumber);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Parses_a_valid_td1_id_card_mrz()
    {
        var result = _parser.TryParse([Td1Line1, Td1Line2, Td1Line3]);

        Assert.NotNull(result);
        Assert.Equal(MrzFormat.Td1, result.Format);
        Assert.Equal("UTO", result.IssuingCountry);
        Assert.Equal("MUSTERMANN", result.Surname);
        Assert.Equal("ERIKA", result.GivenNames);
        Assert.Equal("D23145890", result.DocumentNumber);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Detects_mrz_lines_among_unrelated_ocr_noise()
    {
        var result = _parser.TryParse(["ANY REPUBLIC", "PASSPORT", Td3Line1, Td3Line2, "SIGNATURE"]);

        Assert.NotNull(result);
        Assert.Equal(MrzFormat.Td3, result.Format);
    }

    [Fact]
    public void Flags_a_tampered_document_number_as_invalid_but_still_parses()
    {
        var tamperedLine2 = "L898902C46UTO7408122F1204159<<<<<<<<<<<<<<08";

        var result = _parser.TryParse([Td3Line1, tamperedLine2]);

        Assert.NotNull(result);
        Assert.False(result.CheckDigits.DocumentNumberValid);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Flags_a_tampered_composite_check_digit_as_invalid()
    {
        var tamperedLine2 = "L898902C36UTO7408122F1204159<<<<<<<<<<<<<<09";

        var result = _parser.TryParse([Td3Line1, tamperedLine2]);

        Assert.NotNull(result);
        Assert.True(result.CheckDigits.DocumentNumberValid);
        Assert.False(result.CheckDigits.CompositeValid);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Returns_null_when_no_mrz_shaped_lines_are_present()
    {
        var result = _parser.TryParse(["JUST SOME", "RANDOM OCR", "TEXT LINES"]);

        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_for_an_empty_line_set()
    {
        var result = _parser.TryParse([]);

        Assert.Null(result);
    }

    [Fact]
    public void Normalizes_lowercase_and_whitespace_noise_before_matching()
    {
        var noisyLine1 = Td3Line1.ToLowerInvariant();
        var noisyLine2 = "  " + Td3Line2 + "  ";

        var result = _parser.TryParse([noisyLine1, noisyLine2]);

        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }
}
