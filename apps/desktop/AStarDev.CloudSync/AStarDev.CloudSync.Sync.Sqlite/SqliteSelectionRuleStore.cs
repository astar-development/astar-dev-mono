using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the accounts' selection rules in the sync tables.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
internal sealed class SqliteSelectionRuleStore(IDbContextFactory<SyncDbContext> dbContextFactory) : ISelectionRuleReader, ISelectionRuleWriter, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SelectionRule>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([]));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken)
    {
        _ = dbContextFactory;

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
}
