namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>A rule that maps a set of path keywords to a <see cref="FileClassification"/>.</summary>
public sealed record FileClassificationRule(IReadOnlyList<string> Keywords, FileClassification Classification);
