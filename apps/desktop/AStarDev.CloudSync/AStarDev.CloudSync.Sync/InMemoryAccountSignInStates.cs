using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>A thread-safe in-memory <see cref="IAccountSignInStates"/>.</summary>
internal sealed class InMemoryAccountSignInStates : IAccountSignInStates
{
    private readonly ConcurrentDictionary<AccountId, AccountSignInState> states = new();

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public AccountSignInState StateOf(AccountId accountId) => states.TryGetValue(accountId, out var state) ? state : AccountSignInStateFactory.CreateSignedIn();

    /// <inheritdoc />
    public void Record(AccountId accountId, AccountSignInState state)
    {
        states[accountId] = state;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Forget(AccountId accountId)
    {
        _ = states.TryRemove(accountId, out _);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
