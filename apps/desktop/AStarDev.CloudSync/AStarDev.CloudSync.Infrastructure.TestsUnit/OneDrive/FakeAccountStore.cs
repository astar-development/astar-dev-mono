using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeAccountStore : IAccountStore
{
    private readonly List<AccountRegistration> savedRegistrations = [];

    public Func<AccountRegistration, AccountAddOutcome> Save { get; init; } = _ => AccountAddOutcomeFactory.CreateAdded(TestSignIns.AccountId);

    public IReadOnlyList<AccountRegistration> SavedRegistrations => savedRegistrations;

    public Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken)
    {
        savedRegistrations.Add(registration);

        return Task.FromResult(Save(registration));
    }

    public Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken) => Task.FromResult(Result.Success<Option<StoredAccount>, ErrorResponse>(Option.None<StoredAccount>()));

    public Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([]));
}
