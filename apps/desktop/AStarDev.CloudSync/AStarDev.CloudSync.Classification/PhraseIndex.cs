namespace AStarDev.CloudSync.Classification;

/// <summary>The terms of one vocabulary keyed by their tokenised phrase, built once so a match never re-tokenises a term.</summary>
public sealed class PhraseIndex
{
    internal PhraseIndex(Dictionary<string, ClassificationTerm[]> phrases, int longestPhrase)
    {
        Phrases = phrases.GetAlternateLookup<ReadOnlySpan<char>>();
        LongestPhrase = longestPhrase;
    }

    internal Dictionary<string, ClassificationTerm[]>.AlternateLookup<ReadOnlySpan<char>> Phrases { get; }

    internal int LongestPhrase { get; }
}
