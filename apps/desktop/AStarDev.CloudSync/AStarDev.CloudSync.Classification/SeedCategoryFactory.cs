namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="SeedCategory"/> values.</summary>
public static class SeedCategoryFactory
{
    /// <summary>Creates a seed category. A blank name becomes Unclassified.</summary>
    /// <param name="name">The name.</param>
    public static SeedCategory Create(string name) => new(ClassificationText.Collapse(name) is { Length: > 0 } collapsed ? collapsed : ClassificationCategoryFactory.UnclassifiedName);
}
