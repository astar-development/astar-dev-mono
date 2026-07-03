using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

public sealed record DeltaResult(List<DeltaItem> Items, Option<string> NextDeltaLink, bool HasMorePages);
