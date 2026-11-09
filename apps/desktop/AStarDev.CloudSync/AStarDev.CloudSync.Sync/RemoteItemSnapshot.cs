using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>One item as the change feed reported it (#1398).</summary>
public sealed record RemoteItemSnapshot
{
    internal RemoteItemSnapshot(RemoteItemId id, Option<RemoteItemId> parentId, string name, RemoteItemKind kind, DateTimeOffset lastModified, string eTag)
    {
        Id = id;
        ParentId = parentId;
        Name = name;
        Kind = kind;
        LastModified = lastModified;
        ETag = eTag;
    }

    /// <summary>The item's id.</summary>
    public RemoteItemId Id { get; }

    /// <summary>The parent's id; none for the root.</summary>
    public Option<RemoteItemId> ParentId { get; }

    /// <summary>The item's name.</summary>
    public string Name { get; }

    /// <summary>File, folder or deleted.</summary>
    public RemoteItemKind Kind { get; }

    /// <summary>When it last changed on the provider.</summary>
    public DateTimeOffset LastModified { get; }

    /// <summary>The entity tag; it changes on any change, metadata included.</summary>
    public string ETag { get; }

    /// <summary>Returns a redacted form, so names never reach logs by accident.</summary>
    public override string ToString() => "RemoteItemSnapshot(redacted)";
}
