using System.Text.Json;
using IdExtractor.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IdExtractor.SharedKernel.Auditing;

/// <summary>
/// EF Core interceptor that stamps <see cref="IAuditable"/> entities with who/when on
/// save, and writes one <see cref="AuditLog"/> row per added/modified/deleted entity.
/// Register an <see cref="ICurrentActor"/> and this interceptor on every module's
/// DbContext that needs audit tracking.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentActor currentActor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditInfo(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditInfo(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditInfo(DbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = currentActor.ActorId;
        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State is EntityState.Added)
                {
                    auditable.CreatedAtUtc = now;
                    auditable.CreatedBy = actorId;
                }
                else if (entry.State is EntityState.Modified)
                {
                    auditable.ModifiedAtUtc = now;
                    auditable.ModifiedBy = actorId;
                }
            }

            auditLogs.Add(BuildAuditLog(entry, now, actorId));
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }

    private static AuditLog BuildAuditLog(EntityEntry entry, DateTimeOffset now, string? actorId)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(Entity.Id));
        var entityId = idProperty?.CurrentValue?.ToString() ?? "unknown";

        var changes = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (IsSensitive(property.Metadata))
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    changes[property.Metadata.Name] = property.CurrentValue;
                    break;
                case EntityState.Modified when property.IsModified:
                    changes[property.Metadata.Name] = new { Old = property.OriginalValue, New = property.CurrentValue };
                    break;
                case EntityState.Deleted:
                    changes[property.Metadata.Name] = property.OriginalValue;
                    break;
            }
        }

        return new AuditLog
        {
            EntityName = entry.Entity.GetType().Name,
            EntityId = entityId,
            Action = entry.State.ToString(),
            ChangesJson = changes.Count > 0 ? JsonSerializer.Serialize(changes) : null,
            TimestampUtc = now,
            ActorId = actorId
        };
    }

    private static bool IsSensitive(IProperty property) =>
        property.PropertyInfo?.GetCustomAttributes(typeof(SensitiveAttribute), inherit: true).Length > 0;
}
