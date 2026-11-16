namespace AStarDev.CloudSync.Classification;

/// <summary>The categories and terms a classification run matches against.</summary>
public sealed record ClassificationVocabulary
{
    internal ClassificationVocabulary(IReadOnlyList<ClassificationCategory> categories, IReadOnlyList<ClassificationTerm> terms, PhraseIndex phraseIndex)
    {
        Categories = categories;
        Terms = terms;
        PhraseIndex = phraseIndex;
    }

    /// <summary>The categories.</summary>
    public IReadOnlyList<ClassificationCategory> Categories { get; }

    /// <summary>The terms indexed by phrase, built once with the vocabulary.</summary>
    public PhraseIndex PhraseIndex { get; }

    /// <summary>The terms.</summary>
    public IReadOnlyList<ClassificationTerm> Terms { get; }
}
