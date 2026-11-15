using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>How a sync pair is doing: its status, how many files it holds and when it last finished a sync.</summary>
/// <param name="Status">The sync pair's sync status.</param>
/// <param name="FileCount">Files in the pair.</param>
/// <param name="LastSync">When the pair last finished a sync, if ever.</param>
public sealed record SyncPairState(SyncStatus Status, int FileCount, Option<DateTimeOffset> LastSync);
