namespace IdExtractor.SharedKernel.Auditing;

/// <summary>
/// Identifies who is making the current request, for audit stamping. Implemented in
/// the API host by reading the caller's API-key identity; kept as an abstraction here
/// so SharedKernel has no dependency on ASP.NET Core's HTTP types.
/// </summary>
public interface ICurrentActor
{
    string? ActorId { get; }
}
