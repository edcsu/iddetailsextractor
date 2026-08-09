namespace IdExtractor.SharedKernel.Entities;

/// <summary>
/// Implemented by entities whose creation/modification metadata is stamped
/// automatically by <see cref="IdExtractor.SharedKernel.Auditing.AuditSaveChangesInterceptor"/>.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? ModifiedAtUtc { get; set; }
    string? ModifiedBy { get; set; }
}
