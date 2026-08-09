namespace IdExtractor.SharedKernel.Entities;

/// <summary>
/// Base type for all persisted entities. Ids are GUIDv7 (time-ordered), generated
/// client-side so they're available before the entity is saved and stay index-friendly
/// in Postgres.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}
