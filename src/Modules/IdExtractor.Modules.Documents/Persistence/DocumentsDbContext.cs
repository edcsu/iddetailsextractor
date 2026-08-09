using IdExtractor.SharedKernel.Auditing;
using Microsoft.EntityFrameworkCore;

namespace IdExtractor.Modules.Documents.Persistence;

/// <summary>Owns the "documents" Postgres schema — persisted extraction records and their audit trail.</summary>
public sealed class DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : DbContext(options)
{
    public DbSet<ExtractionRecord> ExtractionRecords => Set<ExtractionRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("documents");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);
    }
}
