namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>A keyword entry returned from the repository, including the database identifier.</summary>
public sealed record FileClassificationKeywordEntry(int Id, FileClassificationKeyword Keyword);
