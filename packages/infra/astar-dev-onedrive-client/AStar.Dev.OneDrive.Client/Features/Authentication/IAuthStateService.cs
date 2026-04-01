namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     Publishes auth state transitions for UI subscribers (AU-05).
///     Observables fire from background threads; subscribers must use <c>ObserveOn(RxApp.MainThreadScheduler)</c>.
/// </summary>
public interface IAuthStateService
{
    /// <summary>
    ///     Observable fired whenever an account's auth state changes (e.g. token refresh fails).
    ///     Fires from the token-refresh background thread; subscribers must marshal to UI thread.
    /// </summary>
    IObservable<(Guid AccountId, AccountAuthState NewState)> AccountAuthStateChanged { get; }

    /// <summary>
    ///     Publish an auth state transition.
    /// </summary>
    void PublishAuthStateChange(Guid accountId, AccountAuthState newState);
}
