namespace IdExtractor.Api.Infrastructure.Auth;

/// <summary>
/// One configured caller. <see cref="HashedKey"/> is the uppercase hex SHA-256 of the
/// raw key — raw keys are never stored in config.
/// </summary>
public sealed class ApiKeyEntry
{
    public required string Name { get; init; }
    public required string HashedKey { get; init; }
}

public sealed class ApiKeysOptions
{
    public const string SectionName = "ApiKeys";

    public List<ApiKeyEntry> Keys { get; init; } = [];
}
