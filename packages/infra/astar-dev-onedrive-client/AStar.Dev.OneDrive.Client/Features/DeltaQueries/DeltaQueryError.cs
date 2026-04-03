namespace AStar.Dev.OneDrive.Client.Features.DeltaQueries;

/// <summary>Base discriminated union for delta query failures.</summary>
public abstract record DeltaQueryError(string Message);

/// <summary>Returned when the stored delta token has expired (HTTP 410 Gone — SE-10).</summary>
public sealed record DeltaTokenExpiredError(string Message) : DeltaQueryError(Message);

/// <summary>Returned when Graph API throttles after exhausting retries (HTTP 429 — EH-02).</summary>
public sealed record DeltaQueryThrottledError(string Message) : DeltaQueryError(Message);

/// <summary>Returned for any other Graph API failure.</summary>
public sealed record DeltaQueryFailedError(string Message) : DeltaQueryError(Message);

/// <summary>Factory for <see cref="DeltaQueryError"/> subtypes.</summary>
public static class DeltaQueryErrorFactory
{
    /// <summary>Creates a <see cref="DeltaTokenExpiredError"/>.</summary>
    public static DeltaTokenExpiredError TokenExpired()
        => new("Delta token has expired. A full re-sync is required (SE-10).");

    /// <summary>Creates a <see cref="DeltaQueryThrottledError"/>.</summary>
    public static DeltaQueryThrottledError Throttled(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new(reason);
    }

    /// <summary>Creates a <see cref="DeltaQueryFailedError"/>.</summary>
    public static DeltaQueryFailedError Failed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new(reason);
    }
}
