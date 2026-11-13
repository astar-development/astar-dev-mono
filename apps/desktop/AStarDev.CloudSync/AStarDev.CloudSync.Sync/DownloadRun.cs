using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

internal sealed class DownloadRun(AccountId accountId, IDirectoryInfo root, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> byPath, BaselineBatch batch)
{
    internal AccountId AccountId { get; } = accountId;

    internal IDirectoryInfo Root { get; } = root;

    internal IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> ByPath { get; } = byPath;

    internal BaselineBatch Batch { get; } = batch;

    internal ConcurrentBag<DownloadItemOutcome> Outcomes { get; } = [];

    internal StrongBox<int> Stopped { get; } = new();
}
