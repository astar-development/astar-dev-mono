using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>A page of items returned from a Microsoft Graph delta query.</summary>
/// <param name="Items">The items returned in this page.</param>
/// <param name="NextDeltaLink">The delta link to resume from on the next sync pass, when available.</param>
/// <param name="HasMorePages">Whether additional pages are available beyond this one.</param>
public sealed record DeltaResult(IReadOnlyList<DeltaItem> Items, Option<string> NextDeltaLink, bool HasMorePages);
