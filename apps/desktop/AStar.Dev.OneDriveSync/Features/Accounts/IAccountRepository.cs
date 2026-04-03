using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

/// <summary>
///     Data access contract for <see cref="Account"/> entities (AM-01, AM-08, S002).
/// </summary>
public interface IAccountRepository
{
    /// <summary>Returns <c>true</c> if at least one account is persisted.</summary>
    Task<bool> HasAnyAsync(CancellationToken ct = default);

    /// <summary>Returns all persisted accounts, ordered by <see cref="Account.DisplayName"/>.</summary>
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns the account with <paramref name="id"/>, or <c>null</c> if not found.</summary>
    Task<Account?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Persists a new account row.</summary>
    Task AddAsync(Account account, CancellationToken ct = default);

    /// <summary>Persists all modified properties of an existing account row.</summary>
    Task UpdateAsync(Account account, CancellationToken ct = default);

    /// <summary>Deletes the account with <paramref name="id"/> and all cascade-linked rows (AM-09).</summary>
    Task RemoveAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all local sync paths registered across all accounts (AM-07 overlap check).</summary>
    Task<IReadOnlyList<(Guid AccountId, string LocalSyncPath)>> GetAllSyncPathsAsync(CancellationToken ct = default);
}
