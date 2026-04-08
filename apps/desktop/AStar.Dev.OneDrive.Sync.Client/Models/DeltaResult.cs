namespace AStar.Dev.OneDrive.Sync.Client.Models;

public sealed record DeltaResult(List<DeltaItem> Items, string? NextDeltaLink, bool HasMorePages);
