using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts;

/// <summary>Saves and reads the accounts CloudSync knows about. Every method throws <see cref="OperationCanceledException"/> when its token is already cancelled, and changes nothing.</summary>
public interface IAccountStore
{
    /// <summary>Saves the account. An account with the same provider and key, ignoring case, is not added twice: its profile is refreshed from <paramref name="registration"/> and its id and settings are kept.</summary>
    /// <param name="registration">The account details from the sign-in.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns><see cref="AccountAddOutcome.Added"/> for a new account, <see cref="AccountAddOutcome.ReAuthenticated"/> for an existing one, or <see cref="AccountAddOutcome.Failed"/> with <see cref="AccountAddFailure.PersistFailed"/>.</returns>
    Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken);

    /// <summary>Finds the account with the given provider and key, ignoring case.</summary>
    /// <param name="provider">The provider the account belongs to.</param>
    /// <param name="key">The provider's identifier for the account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken);

    /// <summary>Reads every saved account.</summary>
    /// <remarks>The order of the accounts is not specified.</remarks>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Deletes the account. Deleting an account that does not exist succeeds, so a repeated removal is safe.</summary>
    /// <param name="accountId">The account to delete.</param>
    /// <param name="cancellationToken">Cancels the delete.</param>
    /// <returns><see cref="Unit"/> once the account is gone, or the error that stopped the delete.</returns>
    Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Saves the account's latest storage usage after a quota refresh and leaves every other field alone. Updating an account that does not exist succeeds and changes nothing, so a refresh racing a removal is safe.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="storage">The storage usage the provider reported.</param>
    /// <param name="cancellationToken">Cancels the update.</param>
    /// <returns><see cref="Unit"/> once saved, or the error that stopped the update.</returns>
    Task<Result<Unit, ErrorResponse>> UpdateStorageAsync(AccountId accountId, StorageUsage storage, CancellationToken cancellationToken);
}
