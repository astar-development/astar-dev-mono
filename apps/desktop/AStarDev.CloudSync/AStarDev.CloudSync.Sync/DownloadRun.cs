using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

internal sealed class DownloadRun(AccountId accountId, IDirectoryInfo root, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> byPath, BaselineBatch batch, PhaseTally tally)
{
    internal AccountId AccountId { get; } = accountId;

    internal IDirectoryInfo Root { get; } = root;

    internal IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> ByPath { get; } = byPath;

    internal BaselineBatch Batch { get; } = batch;

    internal PhaseTally Tally { get; } = tally;

    internal ConcurrentBag<DownloadItemOutcome> Outcomes { get; } = [];

    internal StrongBox<int> Stopped { get; } = new();
}
