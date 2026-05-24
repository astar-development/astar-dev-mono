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

    private static HashSet<string> Tokenise(string remotePath)
        => remotePath.Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                     .Select(t => t.ToLowerInvariant())
                     .ToHashSet();
}
