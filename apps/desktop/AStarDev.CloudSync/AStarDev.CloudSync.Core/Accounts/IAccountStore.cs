using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Core.Accounts;

/// <summary>Saves and reads the accounts CloudSync knows about.</summary>
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
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken);
}
