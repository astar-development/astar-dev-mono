using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeAccountSignInStates : IAccountSignInStates
{
    private readonly Dictionary<AccountId, AccountSignInState> states = [];

    public event EventHandler? Changed;

    public AccountSignInState StateOf(AccountId accountId) => states.TryGetValue(accountId, out var state) ? state : AccountSignInStateFactory.CreateSignedIn();

    public void Record(AccountId accountId, AccountSignInState state)
    {
        states[accountId] = state;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Forget(AccountId accountId)
    {
        _ = states.Remove(accountId);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
