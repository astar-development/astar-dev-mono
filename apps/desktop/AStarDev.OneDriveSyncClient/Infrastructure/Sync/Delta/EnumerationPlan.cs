namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>How the next sync pass should discover remote changes.</summary>
public abstract record EnumerationPlan;

/// <summary>A full remote walk is required.</summary>
/// <param name="Reason">One of <see cref="WalkReasons"/>.</param>
public sealed record WalkRequired(string Reason) : EnumerationPlan;

/// <summary>Ask Graph what changed since the stored delta link before deciding whether to walk.</summary>
/// <param name="DeltaLink">The stored delta link.</param>
public sealed record CheckForChanges(string DeltaLink) : EnumerationPlan;
