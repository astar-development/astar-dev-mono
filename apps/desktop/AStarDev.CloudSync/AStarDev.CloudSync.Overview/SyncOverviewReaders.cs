using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Overview;

/// <summary>The readers the overview source combines, grouped so its constructor stays small.</summary>
/// <param name="SignIn">Tells which accounts must sign in again.</param>
/// <param name="Pairs">Reads each account's sync pairs.</param>
/// <param name="Runs">Tells what each account's sync runs did.</param>
/// <param name="Paused">Tells which accounts the user paused.</param>
/// <param name="Baseline">Counts each account's synced folders and files with a count query.</param>
/// <param name="Activity">Reads the newest activity across every account.</param>
internal sealed record SyncOverviewReaders(IAccountSignInStates SignIn, ISyncPairReader Pairs, ISyncRunStatusSource Runs, IAccountPauseReader Paused, ISyncBaselineCounter Baseline, IActivityLogReader Activity);
