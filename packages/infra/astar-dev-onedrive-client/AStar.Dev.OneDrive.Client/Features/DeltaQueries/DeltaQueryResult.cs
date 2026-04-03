namespace AStar.Dev.OneDrive.Client.Features.DeltaQueries;

/// <summary>
///     The result of a successful delta query page iteration.
///     <see cref="NextDeltaToken"/> is the full <c>@odata.deltaLink</c> URL for the next incremental call.
/// </summary>
public sealed record DeltaQueryResult(IReadOnlyList<DeltaItem> Items, string NextDeltaToken, bool IsFullSync);

/// <summary>Factory for <see cref="DeltaQueryResult"/>.</summary>
public static class DeltaQueryResultFactory
{
    /// <summary>Creates a <see cref="DeltaQueryResult"/>.</summary>
    public static DeltaQueryResult Create(IReadOnlyList<DeltaItem> items, string nextDeltaToken, bool isFullSync)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(nextDeltaToken);

        return new DeltaQueryResult(items, nextDeltaToken, isFullSync);
    }
}
