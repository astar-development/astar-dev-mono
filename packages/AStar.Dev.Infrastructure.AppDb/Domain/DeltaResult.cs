using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

public sealed record DeltaResult(List<DeltaItem> Items, Option<string> NextDeltaLink, bool HasMorePages);
