namespace AStarDev.SingleInstance;

/// <summary>Makes sure only one instance of an app runs per user.</summary>
public interface ISingleInstanceGuard
{
    /// <summary>Tries to become the running instance; if another instance holds the lock, asks it to show its window.</summary>
    /// <param name="options">Where the lock and socket live.</param>
    /// <param name="cancellationToken">Cancels the claim.</param>
    /// <returns>The claim. Only <see cref="InstanceClaim.Primary"/> and <see cref="InstanceClaim.LockUnavailable"/> mean the app should keep starting.</returns>
    Task<InstanceClaim> ClaimAsync(SingleInstanceOptions options, CancellationToken cancellationToken);
}
