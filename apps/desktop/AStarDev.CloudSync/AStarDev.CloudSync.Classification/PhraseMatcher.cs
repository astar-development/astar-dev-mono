namespace AStarDev.CloudSync.Classification;

/// <summary>Finds vocabulary terms in a token sequence, longest phrase first.</summary>
public static class PhraseMatcher
{
    /// <summary>Matches against a prebuilt index. A token belongs to at most one match and the longest phrase at each position wins. Never throws.</summary>
    /// <param name="tokens">Tokens from <see cref="ClassificationTokeniser" />.</param>
    /// <param name="index">The index of the terms to look for.</param>
    /// <returns>Every term sharing the matched phrase, in token order.</returns>
    public static IReadOnlyList<ClassificationTerm> Match(IReadOnlyList<string> tokens, PhraseIndex index)
    {
        var matches = new List<ClassificationTerm>();
        var position = 0;

        while (position < tokens.Count)
        {
            var length = Enumerable.Range(1, Math.Min(index.LongestPhrase, tokens.Count - position)).Reverse().FirstOrDefault(size => index.Phrases.Contains(string.Join(' ', tokens.Skip(position).Take(size))));
            matches.AddRange(length == 0 ? [] : index.Phrases[string.Join(' ', tokens.Skip(position).Take(length))]);
            position += Math.Max(length, 1);
        }

        return matches;
    }

    /// <summary>Matches terms against the tokens by building a throwaway index; prefer the overload taking a <see cref="PhraseIndex" /> when matching repeatedly.</summary>
    /// <param name="tokens">Tokens from <see cref="ClassificationTokeniser" />.</param>
    /// <param name="terms">The terms to look for; a term whose text has no tokens never matches.</param>
    /// <returns>Every term sharing the matched phrase, in token order.</returns>
    public static IReadOnlyList<ClassificationTerm> Match(IReadOnlyList<string> tokens, IReadOnlyList<ClassificationTerm> terms) => Match(tokens, PhraseIndexFactory.Create(terms));
}
