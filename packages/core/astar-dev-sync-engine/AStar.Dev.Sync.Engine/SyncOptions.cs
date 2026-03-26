namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Global sync engine configuration.
/// </summary>
public sealed class SyncOptions
{
    /// <summary>Default maximum concurrent upload/download operations per account.</summary>
    public const int DefaultMaxConcurrency = 8;

    /// <summary>Default automatic sync interval.</summary>
    public static readonly TimeSpan DefaultSyncInterval = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum number of concurrent upload/download operations per account (SE-02).
    /// Individual accounts may override this via <see cref="AccountSyncOptions.MaxConcurrency"/>.
    /// </summary>
    public int MaxConcurrency { get; init; } = DefaultMaxConcurrency;

    /// <summary>
    /// Interval between automatic sync runs (SE-04).
    /// With N accounts, each account's schedule is staggered by <c>Interval / N</c> (SE-05).
    /// </summary>
    public TimeSpan SyncInterval { get; init; } = DefaultSyncInterval;
}
