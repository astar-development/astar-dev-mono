namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Classifies a remote file path against a set of configured rules.</summary>
public static class FileClassifier
{
    private static readonly char[] Separators = ['/', '-', '_', '.', ' '];

    /// <summary>
    /// Tokenises <paramref name="remotePath"/> and evaluates each rule, returning all matching classifications.
    /// Returns a single "Unclassified" sentinel when no rules match or the rule list is empty.
    /// </summary>
    public static IReadOnlyList<FileClassification> Classify(string remotePath, IReadOnlyList<FileClassificationRule> rules)
    {
        var tokens = Tokenise(remotePath);
        var matches = rules
            .Where(rule => rule.Keywords.Any(kw => tokens.Contains(kw.ToLowerInvariant())))
            .Select(rule => rule.Classification)
            .ToList();

        if(matches.Count == 0)
            return [FileClassificationFactory.CreateUnclassified()];

        return matches.AsReadOnly();
    }

    /// <summary>
    /// Tokenises <paramref name="remotePath"/> and matches each <see cref="KeywordMapping"/> whose keyword appears in the tokens.
    /// Returns an empty list when no mappings match — callers such as <see cref="ClassificationCombiner"/> are responsible for the Unclassified sentinel.
    /// </summary>
    /// <param name="remotePath">The remote path to classify.</param>
    /// <param name="mappings">The keyword mappings to match against.</param>
    /// <param name="_">Unused — present to disambiguate overload resolution when an untyped empty collection expression <c>[]</c> is passed; the rules overload is preferred in that case.</param>
    public static IReadOnlyList<FileClassification> Classify(string remotePath, IReadOnlyList<KeywordMapping> mappings, bool _ = false)
    {
        var tokens = Tokenise(remotePath);
        var matches = mappings
            .Where(mapping => tokens.Contains(mapping.Keyword.ToLowerInvariant()))
            .Select(mapping => FileClassificationFactory.Create(mapping.Level1, mapping.Level2, mapping.Level3, mapping.IsSpecial))
            .ToList();

        return matches.AsReadOnly();
    }

    private static HashSet<string> Tokenise(string remotePath)
        => remotePath.Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                     .Select(t => t.ToLowerInvariant())
                     .ToHashSet();
}
