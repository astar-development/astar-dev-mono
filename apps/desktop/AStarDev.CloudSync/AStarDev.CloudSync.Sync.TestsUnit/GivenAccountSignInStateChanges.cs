using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAccountSignInStateChanges : IDisposable
{
    private readonly AccountServices services = new();

    [Fact]
    public void when_a_state_is_recorded_or_an_account_forgotten_then_listeners_are_told()
    {
        var changes = 0;
        services.States.Changed += (_, _) => changes++;
        var accountId = AccountIdFactory.Create(Guid.NewGuid());

        services.States.Record(accountId, AccountSignInStateFactory.CreateSignInRequired());
        services.States.Forget(accountId);

        changes.ShouldBe(2);
    }

    public void Dispose() => services.Dispose();
}
