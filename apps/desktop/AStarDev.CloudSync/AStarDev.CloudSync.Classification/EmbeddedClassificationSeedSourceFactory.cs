namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="EmbeddedClassificationSeedSource"/> values.</summary>
public static class EmbeddedClassificationSeedSourceFactory
{
    private const string DefaultPrefix = "AStarDev.CloudSync.Classification.Seed.";

    /// <summary>Creates the source for the seed files shipped in this assembly.</summary>
    public static EmbeddedClassificationSeedSource CreateDefault() => new(typeof(EmbeddedClassificationSeedSourceFactory).Assembly, DefaultPrefix);
}
