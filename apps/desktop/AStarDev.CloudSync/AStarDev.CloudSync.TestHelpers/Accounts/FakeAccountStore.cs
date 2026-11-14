using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeAccountStore : IAccountStore
{
    public static readonly AccountId AddedAccountId = AccountIdFactory.Create(Guid.Parse("2b9f1c0e-5d43-4a7e-9e61-0c8a7b6d5e4f"));

    private readonly Lock gate = new();
    private readonly List<AccountRegistration> savedRegistrations = [];
    private readonly List<(AccountId AccountId, StorageUsage Storage)> updates = [];

    public Func<AccountRegistration, CancellationToken, Task<AccountAddOutcome>> Save { get; init; } = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateAdded(AddedAccountId));

    public IReadOnlyList<AccountRegistration> SavedRegistrations
    {
        get
        {
            lock (gate)
                return [.. savedRegistrations];
        }
    }

    public Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken)
    {
        lock (gate)
            savedRegistrations.Add(registration);

        return Save(registration, cancellationToken);
    }

    public Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken) => Task.FromResult(Result.Success<Option<StoredAccount>, ErrorResponse>(Option.None<StoredAccount>()));

    public Func<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> All { get; init; } = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([]);

    public Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(All());

    public Func<AccountId, Result<Unit, ErrorResponse>> Remove { get; init; } = _ => Result.Success<Unit, ErrorResponse>(Unit.Instance);

    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Remove(accountId));

    public Func<AccountId, StorageUsage, Result<Unit, ErrorResponse>> Update { get; init; } = (_, _) => Result.Success<Unit, ErrorResponse>(Unit.Instance);

    public IReadOnlyList<(AccountId AccountId, StorageUsage Storage)> Updates
    {
        get
        {
            lock (gate)
                return [.. updates];
        }
    }

    public Task<Result<Unit, ErrorResponse>> UpdateStorageAsync(AccountId accountId, StorageUsage storage, CancellationToken cancellationToken)
    {
        lock (gate)
            updates.Add((accountId, storage));

        return Task.FromResult(Update(accountId, storage));
    }
}
