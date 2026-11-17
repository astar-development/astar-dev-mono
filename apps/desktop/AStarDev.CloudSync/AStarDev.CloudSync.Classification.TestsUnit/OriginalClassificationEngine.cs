namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class OriginalClassificationEngine(IReadOnlyList<ClassificationTerm> terms)
{
    private readonly ILookup<string, ClassificationTerm> phrases = terms.ToLookup(term => string.Join(' ', OriginalClassificationTokeniser.Tokenise(term.Text)), StringComparer.Ordinal);

    private readonly int longestPhrase = terms.Select(term => OriginalClassificationTokeniser.Tokenise(term.Text).Count).DefaultIfEmpty(0).Max();

    internal IReadOnlyList<ClassificationTerm> Match(IReadOnlyList<string> tokens)
    {
        var matches = new List<ClassificationTerm>();
        var position = 0;

        while (position < tokens.Count)
        {
            var length = Enumerable.Range(1, Math.Min(longestPhrase, tokens.Count - position)).Reverse().FirstOrDefault(size => phrases.Contains(string.Join(' ', tokens.Skip(position).Take(size))));
            matches.AddRange(length == 0 ? [] : phrases[string.Join(' ', tokens.Skip(position).Take(length))]);
            position += Math.Max(length, 1);
        }

        return matches;
    }

    internal IReadOnlyList<ClassificationTerm> FolderTerms(string path) => [.. path.Split('/').SkipLast(1).SelectMany(folder => Match(OriginalClassificationTokeniser.Tokenise(folder)))];

    internal IReadOnlyList<ClassificationTerm> FileNameTerms(string path) => Match(OriginalClassificationTokeniser.Tokenise(Path.GetFileNameWithoutExtension(path)));
}
