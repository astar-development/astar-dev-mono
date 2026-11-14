using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps each account's transfer concurrency in the sync tables (#1404). An account with no row has the default. Removing the account removes its row.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and changes that fail, by exception type only.</param>
internal sealed class SqliteTransferConcurrencyStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteTransferConcurrencyStore> logger) : ITransferConcurrencyReader, ITransferConcurrencyWriter, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteTransferConcurrencyStore);

    /// <inheritdoc />
    public Task<Result<TransferConcurrency, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<TransferConcurrency, ErrorResponse>, exception => Failed<TransferConcurrency>(exception, "The account's transfer concurrency could not be read."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> SetAsync(AccountId accountId, TransferConcurrency concurrency, CancellationToken cancellationToken) =>
        Try.RunAsync(() => SaveAsync(accountId, concurrency, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's transfer concurrency could not be saved."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's transfer concurrency could not be removed."));

    private async Task<TransferConcurrency> ReadAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var stored = await dbContext.TransferConcurrencies.AsNoTracking().Where(row => row.AccountId == accountId.Value).Select(row => (int?)row.MaxConcurrentTransfers).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return stored is { } value ? TransferConcurrencyFactory.Create(value) : TransferConcurrencyFactory.CreateDefault();
    }

    private async Task<Unit> SaveAsync(AccountId accountId, TransferConcurrency concurrency, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var updated = await dbContext.TransferConcurrencies.Where(row => row.AccountId == accountId.Value).ExecuteUpdateAsync(setters => setters.SetProperty(row => row.MaxConcurrentTransfers, concurrency.Value), cancellationToken).ConfigureAwait(false);
        if (updated == 0)
        {
            _ = dbContext.TransferConcurrencies.Add(new TransferConcurrencyEntity { AccountId = accountId.Value, MaxConcurrentTransfers = concurrency.Value });
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Unit.Instance;
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.TransferConcurrencies.Where(row => row.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
