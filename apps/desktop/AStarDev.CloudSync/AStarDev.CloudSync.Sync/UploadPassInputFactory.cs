using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

internal static class UploadPassInputFactory
{
    internal static UploadPassInput Create(IDirectoryInfo root, LocalChangeSet changes, IReadOnlyList<SelectionRule> rules, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote) => new(root, changes, rules, baseline, remote);
}
