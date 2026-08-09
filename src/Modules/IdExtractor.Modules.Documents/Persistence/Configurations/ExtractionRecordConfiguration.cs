using System.Text.Json;
using IdExtractor.SharedKernel.Contracts.Extraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IdExtractor.Modules.Documents.Persistence.Configurations;

public sealed class ExtractionRecordConfiguration : IEntityTypeConfiguration<ExtractionRecord>
{
    public void Configure(EntityTypeBuilder<ExtractionRecord> builder)
    {
        builder.ToTable("extraction_records");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(32);

        var fieldsConverter = new ValueConverter<IReadOnlyDictionary<string, ExtractedFieldDto>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, ExtractedFieldDto>>(v, (JsonSerializerOptions?)null)
                 ?? new Dictionary<string, ExtractedFieldDto>());

        var fieldsComparer = new ValueComparer<IReadOnlyDictionary<string, ExtractedFieldDto>>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode());

        builder.Property(e => e.Fields)
            .HasConversion(fieldsConverter, fieldsComparer)
            .HasColumnType("jsonb");

        var mrzConverter = new ValueConverter<MrzResultDto?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => v == null ? null : JsonSerializer.Deserialize<MrzResultDto>(v, (JsonSerializerOptions?)null));

        builder.Property(e => e.Mrz)
            .HasConversion(mrzConverter)
            .HasColumnType("jsonb");

        builder.Property(e => e.RawText).HasColumnType("text");
        builder.Property(e => e.OcrProvider).HasMaxLength(64);
        builder.Property(e => e.ImagePath).HasMaxLength(512);
        builder.Property(e => e.ImageContentType).HasMaxLength(64);
        builder.Property(e => e.CreatedBy).HasMaxLength(128);
        builder.Property(e => e.ModifiedBy).HasMaxLength(128);

        builder.HasIndex(e => e.CreatedAtUtc);
    }
}
