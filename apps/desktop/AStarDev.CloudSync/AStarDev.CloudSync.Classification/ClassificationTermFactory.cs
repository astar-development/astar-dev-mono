using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationTerm"/> values.</summary>
public static class ClassificationTermFactory
{
    /// <summary>Creates a term, or none when the text is blank. The text is composed, trimmed, single spaced and lower case.</summary>
    /// <param name="id">The id.</param>
    /// <param name="text">The text. Blank text gives none.</param>
    /// <param name="categoryId">The category.</param>
    /// <param name="source">Where the term came from.</param>
    /// <param name="status">Active or ignored.</param>
    public static Option<ClassificationTerm> Create(TermId id, string text, CategoryId categoryId, TermSource source, TermStatus status) => ClassificationText.Collapse(text) is { Length: > 0 } collapsed ? Option.Some(new ClassificationTerm(id, collapsed.ToLowerInvariant(), categoryId, source, status)) : Option.None<ClassificationTerm>();
}
