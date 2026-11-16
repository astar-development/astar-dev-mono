namespace AStarDev.CloudSync.Classification;

/// <summary>A named category that terms belong to. The name is trimmed and keeps its case.</summary>
public sealed record ClassificationCategory
{
    internal ClassificationCategory(CategoryId id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>The id.</summary>
    public CategoryId Id { get; }

    /// <summary>The name.</summary>
    public string Name { get; }
}
