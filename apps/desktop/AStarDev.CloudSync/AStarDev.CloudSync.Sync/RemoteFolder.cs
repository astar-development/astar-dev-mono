using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>A folder on the provider (#1396).</summary>
public sealed record RemoteFolder
{
    internal RemoteFolder(RemoteItemId id, string name, RemoteItemId parentId, RemoteFolderChildren children, Option<RemoteFolderVersion> version)
    {
        Id = id;
        Name = name;
        ParentId = parentId;
        Children = children;
        Version = version;
    }

    /// <summary>The folder's id.</summary>
    public RemoteItemId Id { get; }

    /// <summary>The folder's name.</summary>
    public string Name { get; }

    /// <summary>The parent folder's id.</summary>
    public RemoteItemId ParentId { get; }

    /// <summary>Whether it may have child folders.</summary>
    public RemoteFolderChildren Children { get; }

    /// <summary>The folder's entity tag and modified time. Present when the folder came from a create or lookup call; a folder listing does not carry it.</summary>
    public Option<RemoteFolderVersion> Version { get; }

    /// <summary>Returns a redacted form, so names never reach logs by accident.</summary>
    public override string ToString() => "RemoteFolder(redacted)";
}
