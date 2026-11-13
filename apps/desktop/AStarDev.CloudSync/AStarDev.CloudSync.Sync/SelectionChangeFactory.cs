using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SelectionChange"/> cases.</summary>
public static class SelectionChangeFactory
{
    /// <summary>Creates the <see cref="SelectionChange.IncludeFolder"/> case.</summary>
    public static SelectionChange CreateIncludeFolder(LocalRelativePath path, Option<RemoteItemId> remoteId, SyncDirection direction) => new SelectionChange.IncludeFolder(path, remoteId, direction);

    /// <summary>Creates the <see cref="SelectionChange.ExcludeFolder"/> case.</summary>
    public static SelectionChange CreateExcludeFolder(LocalRelativePath path, Option<RemoteItemId> remoteId) => new SelectionChange.ExcludeFolder(path, remoteId);

    /// <summary>Creates the <see cref="SelectionChange.ChangeDirection"/> case.</summary>
    public static SelectionChange CreateChangeDirection(SyncPairId pair, SyncDirection direction) => new SelectionChange.ChangeDirection(pair, direction);

    /// <summary>Creates the <see cref="SelectionChange.RelocateFolders"/> case. Missing moves become an empty list.</summary>
    public static SelectionChange CreateRelocateFolders(IEnumerable<FolderMove>? moves) => new SelectionChange.RelocateFolders([.. moves ?? []]);
}
