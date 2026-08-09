using IdExtractor.SharedKernel.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdExtractor.Modules.Documents.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityName).HasMaxLength(128);
        builder.Property(e => e.EntityId).HasMaxLength(128);
        builder.Property(e => e.Action).HasMaxLength(32);
        builder.Property(e => e.ChangesJson).HasColumnType("jsonb");
        builder.Property(e => e.ActorId).HasMaxLength(128);

        builder.HasIndex(e => e.TimestampUtc);
    }
}
