namespace AStarDev.CloudSync.Classification;

/// <summary>The categories and terms a classification run matches against.</summary>
public sealed record ClassificationVocabulary
{
    internal ClassificationVocabulary(IReadOnlyList<ClassificationCategory> categories, IReadOnlyList<ClassificationTerm> terms)
    {
        Categories = categories;
        Terms = terms;
    }

    /// <summary>The categories.</summary>
    public IReadOnlyList<ClassificationCategory> Categories { get; }

    /// <summary>The terms.</summary>
    public IReadOnlyList<ClassificationTerm> Terms { get; }
}
