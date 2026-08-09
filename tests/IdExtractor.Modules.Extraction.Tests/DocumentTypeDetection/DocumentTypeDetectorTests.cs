using IdExtractor.Modules.Extraction.DocumentTypeDetection;
using IdExtractor.Modules.Extraction.Mrz;
using IdExtractor.SharedKernel.Contracts.Extraction;

namespace IdExtractor.Modules.Extraction.Tests.DocumentTypeDetection;

public class DocumentTypeDetectorTests
{
    private readonly DocumentTypeDetector _detector = new();

    [Fact]
    public void Td3_mrz_is_detected_as_a_passport_with_high_confidence()
    {
        var mrz = new MrzResult(
            MrzFormat.Td3, "P", "UTO", "UTO", "L898902C3", null, "ERIKSSON", "ANNA MARIA",
            new DateOnly(1974, 8, 12), new DateOnly(2012, 4, 15), "F", ["l1", "l2"],
            new MrzCheckDigitResult(true, true, true, true, true), IsValid: true);

        var result = _detector.Detect(mrz, rawText: "");

        Assert.Equal(DocumentType.Passport, result.DocumentType);
        Assert.True(result.Confidence >= 0.9);
    }

    [Fact]
    public void Td1_mrz_is_detected_as_a_national_id()
    {
        var mrz = new MrzResult(
            MrzFormat.Td1, "ID", "UTO", "UTO", "D23145890", null, "MUSTERMANN", "ERIKA",
            new DateOnly(1974, 8, 12), new DateOnly(2012, 4, 15), "F", ["l1", "l2", "l3"],
            new MrzCheckDigitResult(true, true, true, true, null), IsValid: true);

        var result = _detector.Detect(mrz, rawText: "");

        Assert.Equal(DocumentType.NationalId, result.DocumentType);
    }

    [Fact]
    public void An_invalid_mrz_still_yields_a_type_but_with_lower_confidence()
    {
        var mrz = new MrzResult(
            MrzFormat.Td3, "P", "UTO", "UTO", "L898902C3", null, "ERIKSSON", "ANNA MARIA",
            new DateOnly(1974, 8, 12), new DateOnly(2012, 4, 15), "F", ["l1", "l2"],
            new MrzCheckDigitResult(false, true, true, true, true), IsValid: false);

        var result = _detector.Detect(mrz, rawText: "");

        Assert.Equal(DocumentType.Passport, result.DocumentType);
        Assert.True(result.Confidence < 0.9);
    }

    [Theory]
    [InlineData("UNITED STATES OF AMERICA\nDRIVER LICENSE\nCLASS C", DocumentType.DrivingLicence)]
    [InlineData("REPUBLIC OF EXAMPLE\nPASSPORT\nP1234567", DocumentType.Passport)]
    [InlineData("NATIONAL ID CARD\nIDENTITY CARD", DocumentType.NationalId)]
    public void Falls_back_to_keyword_scoring_when_no_mrz_is_present(string rawText, DocumentType expected)
    {
        var result = _detector.Detect(mrz: null, rawText);

        Assert.Equal(expected, result.DocumentType);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void Returns_unknown_when_nothing_matches()
    {
        var result = _detector.Detect(mrz: null, rawText: "SOME RANDOM TEXT WITH NO CLUES");

        Assert.Equal(DocumentType.Unknown, result.DocumentType);
        Assert.Equal(0, result.Confidence);
    }
}
