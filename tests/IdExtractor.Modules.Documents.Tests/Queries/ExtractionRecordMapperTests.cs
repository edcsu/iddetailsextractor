using IdExtractor.Modules.Documents.Persistence;
using IdExtractor.Modules.Documents.Queries;
using IdExtractor.SharedKernel.Contracts.Extraction;

namespace IdExtractor.Modules.Documents.Tests.Queries;

public class ExtractionRecordMapperTests
{
    [Fact]
    public void Maps_every_field_from_the_entity_to_the_dto()
    {
        var id = Guid.CreateVersion7();
        var fields = new Dictionary<string, ExtractedFieldDto> { ["fullName"] = new("ANNA MARIA ERIKSSON", 0.95) };
        var mrz = new MrzResultDto(["line1", "line2"], Valid: true);
        var record = new ExtractionRecord(id, DocumentType.Passport, 0.95, fields, mrz, "raw text", "paddle", "img.png", "image/png");

        var dto = ExtractionRecordMapper.ToDto(record);

        Assert.Equal(id, dto.Id);
        Assert.Equal(DocumentType.Passport, dto.DocumentType);
        Assert.Equal(0.95, dto.DocumentTypeConfidence);
        Assert.Same(fields, dto.Fields);
        Assert.Same(mrz, dto.Mrz);
        Assert.Equal("raw text", dto.RawText);
        Assert.Equal("paddle", dto.OcrProvider);
    }
}
