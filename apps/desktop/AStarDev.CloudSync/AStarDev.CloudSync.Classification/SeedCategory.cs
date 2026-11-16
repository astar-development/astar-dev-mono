namespace AStarDev.CloudSync.Classification;

/// <summary>A category in the shipped seed data.</summary>
public sealed record SeedCategory
{
    internal SeedCategory(string name)
    {
        Name = name;
    }

    /// <summary>The name.</summary>
    public string Name { get; }
}
