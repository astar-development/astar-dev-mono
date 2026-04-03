namespace AStar.Dev.OneDrive.Client.Features.DeltaQueries;

/// <summary>Represents a single item returned by a Graph delta query.</summary>
public sealed record DeltaItem(string Id, string? Name, string? ParentId, DeltaItemType ItemType, string? PreviousName = null);

/// <summary>Factory for <see cref="DeltaItem"/>.</summary>
public static class DeltaItemFactory
{
    /// <summary>Creates a file or folder delta item.</summary>
    public static DeltaItem Create(string id, string name, string? parentId, DeltaItemType itemType, string? previousName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new DeltaItem(id, name, parentId, itemType, previousName);
    }

    /// <summary>Creates a deleted delta item (Graph delta returns only the id for deleted items).</summary>
    public static DeltaItem CreateDeleted(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return new DeltaItem(id, null, null, DeltaItemType.Deleted);
    }
}
