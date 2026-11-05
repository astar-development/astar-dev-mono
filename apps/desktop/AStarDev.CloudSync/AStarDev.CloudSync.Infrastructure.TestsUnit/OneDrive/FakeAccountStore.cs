using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeAccountStore : IAccountStore
{
    private readonly List<AccountRegistration> savedRegistrations = [];

    public Func<AccountRegistration, CancellationToken, Task<AccountAddOutcome>> Save { get; init; } = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateAdded(TestSignIns.AccountId));

    public IReadOnlyList<AccountRegistration> SavedRegistrations => savedRegistrations;

    public Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken)
    {
        savedRegistrations.Add(registration);

        return Save(registration, cancellationToken);
    }

    public Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken) => Task.FromResult(Result.Success<Option<StoredAccount>, ErrorResponse>(Option.None<StoredAccount>()));

    public Func<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> All { get; init; } = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([]);

    public Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(All());
}
