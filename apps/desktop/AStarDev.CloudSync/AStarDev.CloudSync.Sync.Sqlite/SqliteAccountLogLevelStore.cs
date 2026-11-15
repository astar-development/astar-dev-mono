using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps each account's log level in the sync tables (#1411). An account with no row is standard. Removing the account removes its row.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="levels">The running logging, told about every change.</param>
/// <param name="logger">Reports reads and changes that fail, by exception type only.</param>
internal sealed class SqliteAccountLogLevelStore(IDbContextFactory<SyncDbContext> dbContextFactory, AccountLogLevels levels, ILogger<SqliteAccountLogLevelStore> logger) : IAccountLogLevelSettings, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteAccountLogLevelStore);

    /// <inheritdoc />
    public Task<Result<AccountLogLevel, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<AccountLogLevel, ErrorResponse>, exception => Failed<AccountLogLevel>(exception, "The account's log level could not be read."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> SetAsync(AccountId accountId, AccountLogLevel level, CancellationToken cancellationToken) =>
        Try.RunAsync(() => SaveAsync(accountId, level, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's log level could not be saved."));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> ListVerboseAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ListAsync(cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<IReadOnlyList<AccountId>, ErrorResponse>, exception => Failed<IReadOnlyList<AccountId>>(exception, "The verbose accounts could not be read."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's log level could not be removed."));

    private async Task<AccountLogLevel> ReadAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var stored = await dbContext.AccountLogLevels.AsNoTracking().Where(row => row.AccountId == accountId.Value).Select(row => (bool?)row.Verbose).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return stored is true ? AccountLogLevelFactory.CreateVerbose() : AccountLogLevelFactory.CreateStandard();
    }

    private async Task<IReadOnlyList<AccountId>> ListAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var ids = await dbContext.AccountLogLevels.AsNoTracking().Where(row => row.Verbose).Select(row => row.AccountId).ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. ids.Select(AccountIdFactory.Create)];
    }

    private async Task<Unit> SaveAsync(AccountId accountId, AccountLogLevel level, CancellationToken cancellationToken)
    {
        var verbose = level is AccountLogLevel.Verbose;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var updated = await dbContext.AccountLogLevels.Where(row => row.AccountId == accountId.Value).ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Verbose, verbose), cancellationToken).ConfigureAwait(false);
        if (updated == 0)
        {
            _ = dbContext.AccountLogLevels.Add(new AccountLogLevelEntity { AccountId = accountId.Value, Verbose = verbose });
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        levels.Apply(accountId, level);

        return Unit.Instance;
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.AccountLogLevels.Where(row => row.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        levels.Apply(accountId, AccountLogLevelFactory.CreateStandard());

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
