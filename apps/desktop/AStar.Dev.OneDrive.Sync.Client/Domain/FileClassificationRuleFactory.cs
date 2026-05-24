namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="FileClassificationRule"/>.</summary>
public static class FileClassificationRuleFactory
{
    /// <summary>Creates a <see cref="FileClassificationRule"/>.</summary>
    public static FileClassificationRule Create(IReadOnlyList<string> keywords, FileClassification classification) => new(keywords, classification);
}
