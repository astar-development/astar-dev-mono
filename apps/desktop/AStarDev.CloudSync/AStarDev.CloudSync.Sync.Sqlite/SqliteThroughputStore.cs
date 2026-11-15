using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the transfer samples (#1767) in the sync tables for <see cref="ThroughputLimits.Retention"/>. An append stores the sample, then drops the samples that have aged out.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteThroughputStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteThroughputStore> logger) : IThroughputReader, IThroughputWriter
{
    private const string Location = nameof(SqliteThroughputStore);

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> AppendAsync(TransferSample sample, CancellationToken cancellationToken) =>
        Try.RunAsync(() => StoreAsync(sample, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The throughput could not be saved."));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TransferSample>, ErrorResponse>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(since, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<IReadOnlyList<TransferSample>, ErrorResponse>, exception => Failed<IReadOnlyList<TransferSample>>(exception, "The throughput could not be read."));

    private async Task<Unit> StoreAsync(TransferSample sample, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var oldestKept = sample.At.Subtract(ThroughputLimits.Retention).UtcTicks;
        _ = dbContext.ThroughputSamples.Add(new ThroughputSampleEntity { AtUtcTicks = sample.At.UtcTicks, Bytes = sample.Bytes, ElapsedTicks = sample.Elapsed.Ticks });
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.ThroughputSamples.Where(row => row.AtUtcTicks < oldestKept).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private async Task<IReadOnlyList<TransferSample>> ReadAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var earliest = since.UtcTicks;
        var rows = await dbContext.ThroughputSamples.AsNoTracking().Where(row => row.AtUtcTicks >= earliest).OrderBy(row => row.AtUtcTicks).ThenBy(row => row.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. rows.Select(row => TransferSampleFactory.Create(new DateTimeOffset(row.AtUtcTicks, TimeSpan.Zero), row.Bytes, TimeSpan.FromTicks(row.ElapsedTicks)))];
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
