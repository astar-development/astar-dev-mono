namespace AStarDev.CloudSync.Classification;

/// <summary>Finds vocabulary terms in a token sequence, longest phrase first.</summary>
public static class PhraseMatcher
{
    /// <summary>Matches terms against the tokens; a token belongs to at most one match and the longest phrase at each position wins. Never throws.</summary>
    /// <param name="tokens">Tokens from <see cref="ClassificationTokeniser" />.</param>
    /// <param name="terms">The terms to look for; a term whose text has no tokens never matches.</param>
    /// <returns>Every term sharing the matched phrase, in token order.</returns>
    public static IReadOnlyList<ClassificationTerm> Match(IReadOnlyList<string> tokens, IReadOnlyList<ClassificationTerm> terms)
    {
        var index = terms.ToLookup(term => string.Join(' ', ClassificationTokeniser.Tokenise(term.Text)), StringComparer.Ordinal);
        var longest = terms.Select(term => ClassificationTokeniser.Tokenise(term.Text).Count).DefaultIfEmpty(0).Max();
        var matches = new List<ClassificationTerm>();
        var position = 0;

        while (position < tokens.Count)
        {
            var length = Enumerable.Range(1, Math.Min(longest, tokens.Count - position)).Reverse().FirstOrDefault(size => index.Contains(string.Join(' ', tokens.Skip(position).Take(size))));
            matches.AddRange(length == 0 ? [] : index[string.Join(' ', tokens.Skip(position).Take(length))]);
            position += Math.Max(length, 1);
        }

        return matches;
    }
}
