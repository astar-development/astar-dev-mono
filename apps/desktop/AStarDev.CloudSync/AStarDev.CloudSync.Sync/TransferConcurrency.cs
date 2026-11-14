namespace AStarDev.CloudSync.Sync;

/// <summary>How many file transfers one account runs at once, for downloads and uploads together (#1404).</summary>
public sealed record TransferConcurrency
{
    internal TransferConcurrency(int value) => Value = value;

    /// <summary>The number of transfers in flight at once, from <see cref="TransferConcurrencyFactory.Minimum"/> to <see cref="TransferConcurrencyFactory.Maximum"/>.</summary>
    public int Value { get; }
}
