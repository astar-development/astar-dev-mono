using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;

namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>Reloads the overview whenever an account's sign-in state changes or an account is forgotten (startup check, sign-in, removal).</summary>
public sealed class SignInStateOverviewReloader
{
    /// <summary>Starts following the sign-in states.</summary>
    /// <param name="states">The in-memory sign-in states.</param>
    /// <param name="overview">The overview to reload.</param>
    /// <param name="dispatcher">Runs the reload on the UI thread.</param>
    public SignInStateOverviewReloader(IAccountSignInStates states, SyncOverviewViewModel overview, IUiDispatcher dispatcher) => _ = (states, overview, dispatcher);
}
