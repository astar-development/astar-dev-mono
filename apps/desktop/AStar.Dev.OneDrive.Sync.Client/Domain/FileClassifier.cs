namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Classifies a remote file path against a set of configured rules.</summary>
public static class FileClassifier
{
    private static readonly char[] Separators = ['/', '-', '_', '.', '+', ' '];

    /// <summary>
    /// Tokenises <paramref name="remotePath"/> and matches each <see cref="KeywordMapping"/> whose keyword appears in the tokens.
    /// Returns an empty list when no mappings match — callers such as <see cref="ClassificationCombiner"/> are responsible for the Unclassified sentinel.
    /// </summary>
    /// <param name="remotePath">The remote path to classify.</param>
    /// <param name="mappings">The keyword mappings to match against.</param>
    public static IReadOnlyList<FileClassification> Classify(string remotePath, IReadOnlyList<KeywordMapping> mappings)
    {
        var tokens = Tokenise(remotePath);
        var matches = mappings
            .Where(mapping =>
            {
                string kw = mapping.Keyword.ToLowerInvariant();
                if (!kw.Contains(' '))
                    return tokens.Contains(kw);
                string[] words = kw.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                return tokens.Contains(kw) ||
                       tokens.Contains(kw.Replace(" ", string.Empty)) ||
                       words.All(tokens.Contains);
            })
            .Select(mapping => FileClassificationFactory.Create(mapping.Level1, mapping.Level2, mapping.Level3, mapping.IsSpecial))
            .ToList();

        return matches.AsReadOnly();
    }

    private static HashSet<string> Tokenise(string remotePath)
        => [.. remotePath.Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(t => t.ToLowerInvariant())];
}
