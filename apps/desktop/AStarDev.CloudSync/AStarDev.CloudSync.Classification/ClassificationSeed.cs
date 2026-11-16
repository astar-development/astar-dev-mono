namespace AStarDev.CloudSync.Classification;

/// <summary>The shipped seed data: categories and terms, with a version so a changed seed can be detected.</summary>
public sealed record ClassificationSeed
{
    internal ClassificationSeed(string version, IReadOnlyList<SeedCategory> categories, IReadOnlyList<SeedTerm> terms)
    {
        Version = version;
        Categories = categories;
        Terms = terms;
    }

    /// <summary>The seed version.</summary>
    public string Version { get; }

    /// <summary>The categories.</summary>
    public IReadOnlyList<SeedCategory> Categories { get; }

    /// <summary>The terms. Every term names a category in <see cref="Categories"/>.</summary>
    public IReadOnlyList<SeedTerm> Terms { get; }
}
