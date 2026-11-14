namespace AStarDev.CloudSync.Sync;

/// <summary>How long a file the sync removed locally stays in the <c>.sync-trash</c> folder of the sync root (#1583).</summary>
public sealed record LocalTrashOptions
{
    internal LocalTrashOptions(TimeSpan retention) => Retention = retention;

    /// <summary>How long a trashed item is kept before the next download pass removes it for good.</summary>
    public TimeSpan Retention { get; }
}
