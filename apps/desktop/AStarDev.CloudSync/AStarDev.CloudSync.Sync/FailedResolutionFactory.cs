namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FailedResolution"/> values.</summary>
public static class FailedResolutionFactory
{
    /// <summary>Creates a failed resolution.</summary>
    /// <param name="id">The conflict.</param>
    /// <param name="reason">Why it was not resolved.</param>
    public static FailedResolution Create(ConflictId id, ResolutionFailure reason) => new(id, reason);
}
