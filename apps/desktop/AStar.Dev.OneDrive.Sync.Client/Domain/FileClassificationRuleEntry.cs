namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Pairs a persisted <see cref="FileClassificationRule"/> with its database Id.</summary>
public sealed record FileClassificationRuleEntry(int Id, FileClassificationRule Rule);
