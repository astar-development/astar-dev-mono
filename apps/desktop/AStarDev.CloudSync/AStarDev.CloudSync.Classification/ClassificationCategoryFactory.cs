namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationCategory"/> values.</summary>
public static class ClassificationCategoryFactory
{
    /// <summary>The name used when a category name is blank.</summary>
    public const string UnclassifiedName = "Unclassified";

    /// <summary>Creates a category. The name is trimmed and single spaced; a blank name becomes Unclassified.</summary>
    /// <param name="id">The id.</param>
    /// <param name="name">The name.</param>
    public static ClassificationCategory Create(CategoryId id, string name) => new(id, ClassificationText.Collapse(name) is { Length: > 0 } collapsed ? collapsed : UnclassifiedName);
}
