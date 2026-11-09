using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>A thread-safe in-memory <see cref="IAccountSignInStates"/>.</summary>
internal sealed class InMemoryAccountSignInStates : IAccountSignInStates
{
    private readonly ConcurrentDictionary<AccountId, AccountSignInState> states = new();

    /// <inheritdoc />
    public AccountSignInState StateOf(AccountId accountId) => throw new NotImplementedException($"{accountId}{states}");

    /// <inheritdoc />
    public void Record(AccountId accountId, AccountSignInState state) => throw new NotImplementedException($"{accountId}{state}");

    /// <inheritdoc />
    public void Forget(AccountId accountId) => throw new NotImplementedException($"{accountId}");
}
