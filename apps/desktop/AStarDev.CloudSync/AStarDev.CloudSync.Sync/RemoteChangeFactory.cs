using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteChange"/> cases.</summary>
public static class RemoteChangeFactory
{
    /// <summary>Creates the <see cref="RemoteChange.Added"/> case.</summary>
    public static RemoteChange CreateAdded(RemoteItemSnapshot item) => new RemoteChange.Added(item);

    /// <summary>Creates the <see cref="RemoteChange.Modified"/> case.</summary>
    public static RemoteChange CreateModified(RemoteItemSnapshot item) => new RemoteChange.Modified(item);

    /// <summary>Creates the <see cref="RemoteChange.Moved"/> case.</summary>
    public static RemoteChange CreateMoved(RemoteItemSnapshot item, Option<RemoteItemId> oldParentId, string? oldName) => new RemoteChange.Moved(item, oldParentId, oldName?.Trim() ?? string.Empty);

    /// <summary>Creates the <see cref="RemoteChange.Deleted"/> case.</summary>
    public static RemoteChange CreateDeleted(RemoteItemId id) => new RemoteChange.Deleted(id);
}
