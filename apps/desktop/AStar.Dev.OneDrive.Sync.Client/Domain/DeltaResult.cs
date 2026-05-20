using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

public sealed record DeltaResult(List<DeltaItem> Items, Option<string> NextDeltaLink, bool HasMorePages);
