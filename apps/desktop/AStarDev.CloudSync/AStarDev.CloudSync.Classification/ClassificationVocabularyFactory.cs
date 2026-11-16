namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationVocabulary"/> values.</summary>
public static class ClassificationVocabularyFactory
{
    /// <summary>Creates a vocabulary, copying both lists, dropping ignored terms so no signal can emit one, and building the phrase index once.</summary>
    /// <param name="categories">The categories.</param>
    /// <param name="terms">The terms.</param>
    public static ClassificationVocabulary Create(IReadOnlyList<ClassificationCategory> categories, IReadOnlyList<ClassificationTerm> terms)
    {
        IReadOnlyList<ClassificationTerm> active = [.. terms.Where(term => term.Status is not TermStatus.Ignored)];

        return new([.. categories], active, PhraseIndexFactory.Create(active));
    }
}
