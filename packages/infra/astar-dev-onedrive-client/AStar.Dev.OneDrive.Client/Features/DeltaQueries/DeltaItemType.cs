namespace AStar.Dev.OneDrive.Client.Features.DeltaQueries;

/// <summary>Classifies a single item returned by a Graph delta query.</summary>
public enum DeltaItemType
{
    /// <summary>A regular file that was added or modified.</summary>
    File,

    /// <summary>A folder that was added or modified.</summary>
    Folder,

    /// <summary>An item that was deleted on the remote.</summary>
    Deleted,

    /// <summary>A folder that was renamed or moved, detected via the Graph <c>@microsoft.graph.moveOrRenamed</c> annotation (SE-12).</summary>
    FolderRenamed,
}
