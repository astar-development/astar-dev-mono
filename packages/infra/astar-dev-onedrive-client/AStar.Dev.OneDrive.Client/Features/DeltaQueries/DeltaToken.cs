namespace AStar.Dev.OneDrive.Client.Features.DeltaQueries;

/// <summary>
///     Stores the <c>@odata.deltaLink</c> URL returned by Graph after a completed delta sync.
///     Persisted per account/folder in SQLite. A token older than 30 days is considered expired (SE-10).
/// </summary>
public sealed record DeltaToken(string Value, DateTimeOffset AcquiredAt)
{
    /// <summary>Returns <see langword="true"/> when the token is more than 30 days old (SE-10).</summary>
    public bool IsExpired => DateTimeOffset.UtcNow - AcquiredAt > TimeSpan.FromDays(30);
}

/// <summary>Factory for <see cref="DeltaToken"/>.</summary>
public static class DeltaTokenFactory
{
    /// <summary>Creates a <see cref="DeltaToken"/> with the current UTC time as the acquisition time.</summary>
    public static DeltaToken Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new DeltaToken(value, DateTimeOffset.UtcNow);
    }
}
