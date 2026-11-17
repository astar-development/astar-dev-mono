namespace AStarDev.CloudSync.Classification;

/// <summary>Finds vocabulary terms in a token sequence, longest phrase first.</summary>
public static class PhraseMatcher
{
    private const int StackTokenCount = 64;

    /// <summary>Matches against a prebuilt index. A token belongs to at most one match and the longest phrase at each position wins. Never throws.</summary>
    /// <param name="tokens">Tokens from <see cref="ClassificationTokeniser" />.</param>
    /// <param name="index">The index of the terms to look for.</param>
    /// <returns>Every term sharing the matched phrase, in token order.</returns>
    public static IReadOnlyList<ClassificationTerm> Match(IReadOnlyList<string> tokens, PhraseIndex index)
    {
        var matches = new List<ClassificationTerm>();
        MatchInto(matches, tokens, index);

        return matches;
    }

    /// <summary>Matches terms against the tokens by building a throwaway index; prefer the overload taking a <see cref="PhraseIndex" /> when matching repeatedly.</summary>
    /// <param name="tokens">Tokens from <see cref="ClassificationTokeniser" />.</param>
    /// <param name="terms">The terms to look for; a term whose text has no tokens never matches.</param>
    /// <returns>Every term sharing the matched phrase, in token order.</returns>
    public static IReadOnlyList<ClassificationTerm> Match(IReadOnlyList<string> tokens, IReadOnlyList<ClassificationTerm> terms) => Match(tokens, PhraseIndexFactory.Create(terms));

    internal static void MatchInto(List<ClassificationTerm> matches, IReadOnlyList<string> tokens, PhraseIndex index)
    {
        if (tokens.Count == 0 || index.LongestPhrase == 0)
            return;

        var text = string.Join(' ', tokens);
        var starts = tokens.Count < StackTokenCount ? stackalloc int[StackTokenCount] : new int[tokens.Count + 1];
        for (var token = 0; token < tokens.Count; token++)
        {
            starts[token + 1] = starts[token] + (tokens[token]?.Length ?? 0) + 1;
        }

        var position = 0;
        while (position < tokens.Count)
        {
            position += MatchAt(matches, text, starts, position, Math.Min(index.LongestPhrase, tokens.Count - position), index);
        }
    }

    private static int MatchAt(List<ClassificationTerm> matches, string text, ReadOnlySpan<int> starts, int position, int longest, PhraseIndex index)
    {
        for (var size = longest; size > 0; size--)
        {
            if (index.Phrases.TryGetValue(text.AsSpan(starts[position], starts[position + size] - starts[position] - 1), out var terms))
            {
                matches.AddRange(terms);

                return size;
            }
        }

        return 1;
    }
}
