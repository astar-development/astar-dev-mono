using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="accounts">Lists the accounts in the order they were added.</param>
/// <param name="settings">Tells the schedule.</param>
/// <param name="stagger">Spreads the accounts across the interval.</param>
/// <param name="paused">Tells which accounts the user paused.</param>
/// <param name="signIn">Tells which accounts need to sign in.</param>
/// <param name="roots">Tells each account's sync root.</param>
/// <param name="activity">Tells which accounts are syncing.</param>
/// <param name="runner">Runs the passes.</param>
/// <param name="options">Startup delay and check period.</param>
/// <param name="timeProvider">Supplies the clock and the timer.</param>
/// <param name="logger">Logs skipped accounts and the reason, without account details.</param>
internal sealed class SyncScheduler(IAccountStore accounts, ISyncScheduleSettings settings, IStaggerPolicy stagger, IAccountPauseReader paused, IAccountSignInStates signIn, ISyncRootReader roots, ISyncActivitySource activity, ISyncPassRunner runner, SyncSchedulerOptions options, TimeProvider timeProvider, ILogger<SyncScheduler> logger) : ISyncScheduler
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{accounts}{settings}{stagger}{paused}{signIn}{roots}{activity}{runner}{options}{timeProvider}{logger}{cancellationToken}");

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");
}
