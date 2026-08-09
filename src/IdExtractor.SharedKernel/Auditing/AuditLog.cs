namespace IdExtractor.SharedKernel.Auditing;

/// <summary>
/// One row per entity change (create/modify/delete), written automatically by
/// <see cref="AuditSaveChangesInterceptor"/>. <see cref="ChangesJson"/> never contains
/// values from properties marked <see cref="SensitiveAttribute"/>.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string EntityName { get; init; }
    public required string EntityId { get; init; }
    public required string Action { get; init; }
    public string? ChangesJson { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? ActorId { get; init; }
}
