using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class InMemoryAccountStore : IAccountStore
{
    private readonly Lock gate = new();
    private readonly List<StoredAccount> accounts = [];

    public Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            return Task.FromResult(Find(registration.Provider, registration.Key).Match(existing => Refresh(existing, registration), () => Add(registration)));
    }

    public Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            return Task.FromResult(Result.Success<Option<StoredAccount>, ErrorResponse>(Find(provider, key)));
    }

    public Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
            return Task.FromResult(Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([.. accounts]));
    }

    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            _ = accounts.RemoveAll(account => account.Id == accountId);
            
        }

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public Task<Result<Unit, ErrorResponse>> UpdateStorageAsync(AccountId accountId, StorageUsage storage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            var index = accounts.FindIndex(account => account.Id == accountId);
            if (index >= 0)
            {
                var registration = accounts[index].Registration;
                accounts[index] = StoredAccountFactory.Create(accountId, AccountRegistrationFactory.Create(registration.Key, registration.Provider, registration.DisplayName, registration.Email, registration.DriveId, storage));
            }
        }

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    private Option<StoredAccount> Find(CloudProvider provider, ProviderAccountKey key) => accounts
        .Where(account => account.Registration.Provider == provider && string.Equals(account.Registration.Key.Value, key.Value, StringComparison.OrdinalIgnoreCase))
        .Select(Option.Some)
        .DefaultIfEmpty(Option.None<StoredAccount>())
        .First();

    private AccountAddOutcome Refresh(StoredAccount existing, AccountRegistration registration)
    {
        accounts[accounts.IndexOf(existing)] = StoredAccountFactory.Create(existing.Id, registration);

        return AccountAddOutcomeFactory.CreateReAuthenticated(existing.Id);
    }

    private AccountAddOutcome Add(AccountRegistration registration)
    {
        var added = StoredAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), registration);
        accounts.Add(added);

        return AccountAddOutcomeFactory.CreateAdded(added.Id);
    }
}
