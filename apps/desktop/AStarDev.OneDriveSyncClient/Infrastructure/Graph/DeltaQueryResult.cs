namespace AStarDev.OneDriveSyncClient.Infrastructure.Graph;

/// <summary>The outcome of reading changes from the Graph delta endpoint.</summary>
public abstract record DeltaQueryResult;

/// <summary>Changes were read successfully.</summary>
/// <param name="Changes">Every change since the supplied delta link.</param>
/// <param name="NextDeltaLink">The delta link to use on the next pass.</param>
public sealed record DeltaChangesFound(IReadOnlyList<DeltaChange> Changes, string NextDeltaLink) : DeltaQueryResult;

/// <summary>The supplied delta link is no longer usable and a full enumeration is required.</summary>
public sealed record DeltaResyncRequired : DeltaQueryResult;
