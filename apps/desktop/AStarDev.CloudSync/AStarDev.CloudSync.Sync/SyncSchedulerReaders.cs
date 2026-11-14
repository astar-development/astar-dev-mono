using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>What the scheduler reads before it starts a pass: the schedule, the accounts, which are paused, their sign-in state, their roots and what is running.</summary>
/// <param name="Accounts">Lists the accounts in the order they were added.</param>
/// <param name="Settings">Tells the schedule.</param>
/// <param name="Paused">Tells which accounts the user paused.</param>
/// <param name="SignIn">Tells which accounts need to sign in.</param>
/// <param name="Roots">Tells each account's sync root.</param>
/// <param name="Activity">Tells which accounts are syncing.</param>
internal sealed record SyncSchedulerReaders(IAccountStore Accounts, ISyncScheduleSettings Settings, IAccountPauseReader Paused, IAccountSignInStates SignIn, ISyncRootReader Roots, ISyncActivitySource Activity);
