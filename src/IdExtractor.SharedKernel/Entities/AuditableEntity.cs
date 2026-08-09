namespace IdExtractor.SharedKernel.Entities;

/// <summary>
/// Base type for entities that need creation/modification tracking. Combine with
/// <see cref="Auditing.SensitiveAttribute"/> on individual properties to keep their
/// values out of the audit log payload.
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
