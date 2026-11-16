using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="SeedTerm"/> values.</summary>
public static class SeedTermFactory
{
    /// <summary>Creates a seed term, or none when the text is blank.</summary>
    /// <param name="text">The text.</param>
    /// <param name="categoryName">The category name.</param>
    public static Option<SeedTerm> Create(string text, string categoryName) => Create(text, categoryName, SeedTermKindFactory.CreateWord());

    /// <summary>Creates a seed term of the given kind, or none when the text is blank.</summary>
    /// <param name="text">The text.</param>
    /// <param name="categoryName">The category name.</param>
    /// <param name="kind">What the term matches.</param>
    public static Option<SeedTerm> Create(string text, string categoryName, SeedTermKind kind) => ClassificationText.Collapse(text) is { Length: > 0 } collapsedText ? Option.Some(new SeedTerm(collapsedText.ToLowerInvariant(), ClassificationText.Collapse(categoryName), kind)) : Option.None<SeedTerm>();
}
