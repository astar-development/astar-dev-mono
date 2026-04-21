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
    /// Inserts a new account or updates an existing one with the same ID.
    /// </summary>
    /// <param name="account">The account to insert or update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpsertAsync(AccountEntity account, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the account with the specified ID. If no such account exists, does nothing.
    /// </summary>
    /// <param name="id">The ID of the account to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAsync(AccountId id, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the account with the specified ID as the active account. Only one account can be active at a time.
    /// </summary>
    /// <param name="id">The ID of the account to set as active.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SetActiveAccountAsync(AccountId id, CancellationToken cancellationToken);
}
