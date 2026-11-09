using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>The in-memory sign-in state of every account (#1393 decision 2: never persisted).</summary>
public interface IAccountSignInStates
{
    /// <summary>Raised after <see cref="Record"/> or <see cref="Forget"/>, on the caller's thread, so the UI can reload.</summary>
    event EventHandler? Changed;

    /// <summary>The account's state; <see cref="AccountSignInState.SignedIn"/> until something says otherwise.</summary>
    /// <param name="accountId">The account.</param>
    AccountSignInState StateOf(AccountId accountId);

    /// <summary>Records the account's state.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="state">The new state.</param>
    void Record(AccountId accountId, AccountSignInState state);

    /// <summary>Forgets the account, for example after it is removed.</summary>
    /// <param name="accountId">The account.</param>
    void Forget(AccountId accountId);
}
