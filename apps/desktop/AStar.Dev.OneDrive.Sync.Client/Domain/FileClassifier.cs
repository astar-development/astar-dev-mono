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
        => throw new NotImplementedException();
}
