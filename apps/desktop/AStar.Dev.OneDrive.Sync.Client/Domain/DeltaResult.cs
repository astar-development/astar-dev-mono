namespace AStar.Dev.OneDrive.Sync.Client.Domain;

public sealed record DeltaResult(List<DeltaItem> Items, string? NextDeltaLink, bool HasMorePages);
