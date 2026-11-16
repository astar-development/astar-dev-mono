using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="SeedTerm"/> values.</summary>
public static class SeedTermFactory
{
    /// <summary>Creates a seed term, or none when the text is blank.</summary>
    /// <param name="text">The text.</param>
    /// <param name="categoryName">The category name.</param>
    public static Option<SeedTerm> Create(string text, string categoryName) => throw new NotImplementedException($"{text}{categoryName}");
}
