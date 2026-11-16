namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="EmbeddedWordLists"/> values.</summary>
public static class EmbeddedWordListsFactory
{
    private const string DefaultPrefix = "AStarDev.CloudSync.Classification.Lists.";

    /// <summary>Creates the lists shipped in this assembly.</summary>
    public static EmbeddedWordLists CreateDefault() => new(typeof(EmbeddedWordListsFactory).Assembly, DefaultPrefix);
}
