namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

/// <summary>Creates <see cref="DeltaQueryResult"/> cases.</summary>
public static class DeltaQueryResultFactory
{
    /// <summary>Creates a <see cref="DeltaChangesFound"/> case.</summary>
    public static DeltaQueryResult CreateChangesFound(IReadOnlyList<DeltaChange> changes, string nextDeltaLink) => new DeltaChangesFound(changes, nextDeltaLink);

    /// <summary>Creates a <see cref="DeltaResyncRequired"/> case.</summary>
    public static DeltaQueryResult CreateResyncRequired() => new DeltaResyncRequired();
}
