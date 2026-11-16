namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationVocabulary"/> values.</summary>
public static class ClassificationVocabularyFactory
{
    /// <summary>Creates a vocabulary, copying both lists.</summary>
    /// <param name="categories">The categories.</param>
    /// <param name="terms">The terms.</param>
    public static ClassificationVocabulary Create(IReadOnlyList<ClassificationCategory> categories, IReadOnlyList<ClassificationTerm> terms) => throw new NotImplementedException($"{categories}{terms}");
}
