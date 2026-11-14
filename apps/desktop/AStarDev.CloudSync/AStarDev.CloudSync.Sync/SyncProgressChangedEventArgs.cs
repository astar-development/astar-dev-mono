using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>An account's sync progress changed.</summary>
/// <param name="accountId">The account.</param>
/// <param name="progress">The new progress.</param>
public sealed class SyncProgressChangedEventArgs(AccountId accountId, SyncProgress progress) : EventArgs
{
    /// <summary>The account.</summary>
    public AccountId AccountId { get; } = accountId;

    /// <summary>The new progress.</summary>
    public SyncProgress Progress { get; } = progress;
}
