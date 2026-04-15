using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface IAccountRepository
{
    /// <summary>
    /// Returns all accounts in the database. The list is never null but may be empty.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of accounts - may be empty.</returns>
    Task<List<AccountEntity>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the account with the specified ID, or null if no such account exists.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account, or null if not found.</returns>
    Task<AccountEntity?> GetByIdAsync(AccountId id, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a new account or updates an existing one with the same ID. The account is identified by its ID property.
    /// </summary>
    /// <param name="account">The account to insert or update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpsertAsync(AccountEntity account, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the account with the specified ID. If no such account exists, does nothing.
    /// If the deleted account is currently active, there will be no active account after this operation.
    /// Note: this does not delete any associated sync folders or jobs — those will be orphaned and should be cleaned up separately if needed.
    /// This method is used when unlinking an account, so we intentionally keep the sync history intact in case of re-linking the same account later.
    /// </summary>
    /// <param name="id">The ID of the account to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(AccountId id, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the account with the specified ID as the active account. Only one account can be active at a time.
    /// </summary>
    /// <param name="id">The ID of the account to set as active.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetActiveAccountAsync(AccountId id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the delta link for the specified account and folder combination.
    /// </summary>
    /// <param name="accountId">The ID of the account.</param>
    /// <param name="folderId">The ID of the folder.</param>
    /// <param name="deltaLink">The delta link.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateDeltaLinkAsync(AccountId accountId, OneDriveFolderId folderId, string deltaLink, CancellationToken cancellationToken);
}
