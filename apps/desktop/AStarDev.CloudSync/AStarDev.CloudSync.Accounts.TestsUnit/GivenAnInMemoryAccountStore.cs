using AStarDev.CloudSync.TestHelpers.Accounts;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

public sealed class GivenAnInMemoryAccountStore : GivenAnAccountStore
{
    protected override Task<IAccountStore> CreateStoreAsync(CancellationToken cancellationToken) => Task.FromResult<IAccountStore>(new InMemoryAccountStore());
}
