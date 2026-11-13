using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>What comparing one scanned item with the baseline found.</summary>
internal sealed record LocalComparison
{
    internal static readonly LocalComparison Unchanged = new(Option.None<LocalChange>(), Option.None<SyncBaselineEntry>(), hashed: false);

    private LocalComparison(Option<LocalChange> change, Option<SyncBaselineEntry> touched, bool hashed)
    {
        Change = change;
        Touched = touched;
        Hashed = hashed;
    }

    internal Option<LocalChange> Change { get; }

    internal Option<SyncBaselineEntry> Touched { get; }

    internal bool Hashed { get; }

    internal static LocalComparison Changed(LocalChange change, bool hashed) => new(Option.Some(change), Option.None<SyncBaselineEntry>(), hashed);

    internal static LocalComparison TouchedOnly(SyncBaselineEntry refreshed) => new(Option.None<LocalChange>(), Option.Some(refreshed), hashed: true);
}
