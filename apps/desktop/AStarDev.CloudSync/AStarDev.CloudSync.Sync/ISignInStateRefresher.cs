namespace AStarDev.CloudSync.Sync;

/// <summary>Checks every stored account's sign-in state in the background (the startup check, NF-01).</summary>
public interface ISignInStateRefresher
{
    /// <summary>Probes each account and records the result. Accounts whose provider has no probe, or whose probe cannot tell, keep their current state.</summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    Task RefreshAllAsync(CancellationToken cancellationToken);
}
