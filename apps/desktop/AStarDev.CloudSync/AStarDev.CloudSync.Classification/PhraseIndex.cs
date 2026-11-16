namespace AStarDev.CloudSync.Classification;

/// <summary>The terms of one vocabulary keyed by their tokenised phrase, built once so a match never re-tokenises a term.</summary>
public sealed class PhraseIndex
{
    internal PhraseIndex(ILookup<string, ClassificationTerm> phrases, int longestPhrase)
    {
        Phrases = phrases;
        LongestPhrase = longestPhrase;
    }

    internal ILookup<string, ClassificationTerm> Phrases { get; }

    internal int LongestPhrase { get; }
}
