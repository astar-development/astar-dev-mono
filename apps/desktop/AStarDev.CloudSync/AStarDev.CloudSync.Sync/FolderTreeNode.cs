using System.Diagnostics.CodeAnalysis;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>One folder of the selection tree: the local and cloud folders of that name merged (#1397).</summary>
public sealed record FolderTreeNode
{
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Internal constructor called only by FolderTreeNodeFactory; records cannot have an internal primary constructor (#1453).")]
    internal FolderTreeNode(string name, LocalRelativePath path, Option<RemoteItemId> remoteId, FolderOrigin origin, FolderChildren children, SelectionState selection, FolderNameClash nameClash)
    {
        Name = name;
        Path = path;
        RemoteId = remoteId;
        Origin = origin;
        Children = children;
        Selection = selection;
        NameClash = nameClash;
    }

    /// <summary>The folder's name. On disk's casing is used when the folder exists there.</summary>
    public string Name { get; }

    /// <summary>The folder's path relative to the sync root; the cloud folder mirrors it.</summary>
    public LocalRelativePath Path { get; }

    /// <summary>The cloud folder's id, when it exists in the cloud.</summary>
    public Option<RemoteItemId> RemoteId { get; }

    /// <summary>Where the folder exists.</summary>
    public FolderOrigin Origin { get; }

    /// <summary>Whether the folder may have child folders.</summary>
    public FolderChildren Children { get; }

    /// <summary>What the stored selection rules say about the folder.</summary>
    public SelectionState Selection { get; }

    /// <summary>Whether another local folder has the same name ignoring case.</summary>
    public FolderNameClash NameClash { get; }

    /// <summary>Returns a redacted form, so folder names never reach logs by accident.</summary>
    public override string ToString() => "FolderTreeNode(redacted)";
}
