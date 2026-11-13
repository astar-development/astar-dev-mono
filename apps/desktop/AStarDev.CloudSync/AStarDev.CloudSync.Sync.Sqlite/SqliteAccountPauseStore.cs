using System.Data;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the paused accounts in the sync tables: a row means the account is paused. A toggle reads and writes inside one serializable transaction.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and changes that fail, by exception type only.</param>
internal sealed class SqliteAccountPauseStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteAccountPauseStore> logger) : IAccountPauseReader, IAccountPauseWriter, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteAccountPauseStore);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> GetPausedAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<IReadOnlyList<AccountId>, ErrorResponse>, exception => Failed<IReadOnlyList<AccountId>>(exception, "The paused accounts could not be read."));

    /// <inheritdoc />
    public Task<Result<PauseState, ErrorResponse>> ToggleAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => FlipAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<PauseState, ErrorResponse>, exception => Failed<PauseState>(exception, "The account could not be paused or resumed."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's pause could not be removed."));

    private async Task<IReadOnlyList<AccountId>> ReadAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var rows = await dbContext.AccountPauses.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. rows.Select(row => AccountIdFactory.Create(row.AccountId))];
    }

    private async Task<PauseState> FlipAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var existing = await dbContext.AccountPauses.Where(row => row.AccountId == accountId.Value).ToListAsync(cancellationToken).ConfigureAwait(false);
        var state = existing.Count > 0 ? PauseStateFactory.CreateActive() : PauseStateFactory.CreatePaused();
        dbContext.AccountPauses.RemoveRange(existing);

        if (state is PauseState.Paused)
            _ = dbContext.AccountPauses.Add(new AccountPauseEntity { AccountId = accountId.Value });

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return state;
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.AccountPauses.Where(row => row.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
