using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class InMemoryAccountStore : IAccountStore
{
    public Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken) => throw new NotImplementedException($"{registration}{cancellationToken}");

    public Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken) => throw new NotImplementedException($"{provider}{key}{cancellationToken}");

    public Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{cancellationToken}");
}
