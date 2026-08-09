using System.Diagnostics;

namespace IdExtractor.SharedKernel.Telemetry;

/// <summary>
/// Shared <see cref="ActivitySource"/> for tracing extraction and report-generation
/// work across modules. The host registers <see cref="Name"/> with OpenTelemetry
/// tracing; modules start activities from <see cref="Instance"/> so their spans land
/// in the same trace as the request that triggered them.
/// </summary>
public static class IdExtractorActivitySource
{
    public const string Name = "IdExtractor";

    public static readonly ActivitySource Instance = new(Name);
}
