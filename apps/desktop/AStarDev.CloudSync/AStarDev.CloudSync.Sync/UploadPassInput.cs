using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <summary>Everything an upload pass reads before it plans: the sync root, the changes that may be synced, the rules, the baseline rows and the remote index items for the changed paths and their ancestors.</summary>
internal sealed record UploadPassInput
{
    internal UploadPassInput(IDirectoryInfo root, LocalChangeSet changes, IReadOnlyList<SelectionRule> rules, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote)
    {
        Root = root;
        Changes = changes;
        Rules = rules;
        Baseline = baseline;
        Remote = remote;
    }

    internal IDirectoryInfo Root { get; }

    internal LocalChangeSet Changes { get; }

    internal IReadOnlyList<SelectionRule> Rules { get; }

    internal IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> Baseline { get; }

    internal IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> Remote { get; }
}
