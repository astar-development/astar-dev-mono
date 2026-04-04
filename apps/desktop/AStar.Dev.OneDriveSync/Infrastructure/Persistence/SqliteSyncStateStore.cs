using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Sync.Engine.Features.StateTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     SQLite-backed implementation of <see cref="ISyncStateStore"/> using the app's <see cref="AppDbContext"/> (EH-04, EH-05, EH-06).
/// </summary>
internal sealed partial class SqliteSyncStateStore(AppDbContext dbContext, ILogger<SqliteSyncStateStore> logger) : ISyncStateStore
{
    /// <inheritdoc />
    public async Task SetStateAsync(string accountId, SyncAccountState state, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var record = await dbContext.SyncStateRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccountId == accountId, ct)
            .ConfigureAwait(false);

        if (record is null)
        {
            dbContext.SyncStateRecords.Add(new SyncStateRecord { AccountId = accountId, State = state });
        }
        else
        {
            var entry = dbContext.SyncStateRecords.Attach(record);
            entry.Entity.State = state;
            entry.Property(r => r.State).IsModified = true;
        }

        _ = await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        LogStateSet(logger, accountId, state);
    }

    /// <inheritdoc />
    public async Task<SyncAccountState?> GetStateAsync(string accountId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var record = await dbContext.SyncStateRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccountId == accountId, ct)
            .ConfigureAwait(false);

        return record?.State;
    }

    /// <inheritdoc />
    public async Task SaveCheckpointAsync(SyncCheckpoint checkpoint, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        var json = JsonSerializer.Serialize(checkpoint);

        var record = await dbContext.SyncStateRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccountId == checkpoint.AccountId, ct)
            .ConfigureAwait(false);

        if (record is null)
        {
            dbContext.SyncStateRecords.Add(new SyncStateRecord { AccountId = checkpoint.AccountId, State = SyncAccountState.Running, CheckpointJson = json });
        }
        else
        {
            var entry = dbContext.SyncStateRecords.Attach(record);
            entry.Entity.CheckpointJson = json;
            entry.Property(r => r.CheckpointJson).IsModified = true;
        }

        _ = await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        LogCheckpointSaved(logger, checkpoint.AccountId);
    }

    /// <inheritdoc />
    public async Task<SyncCheckpoint?> GetCheckpointAsync(string accountId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var record = await dbContext.SyncStateRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccountId == accountId, ct)
            .ConfigureAwait(false);

        if (record?.CheckpointJson is null)

            return null;

        return JsonSerializer.Deserialize<SyncCheckpoint>(record.CheckpointJson);
    }

    /// <inheritdoc />
    public async Task ClearCheckpointAsync(string accountId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var record = await dbContext.SyncStateRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccountId == accountId, ct)
            .ConfigureAwait(false);

        if (record is null)

            return;

        var entry = dbContext.SyncStateRecords.Attach(record);
        entry.Entity.CheckpointJson = null;
        entry.Property(r => r.CheckpointJson).IsModified = true;

        _ = await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        LogCheckpointCleared(logger, accountId);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sync state set for account {AccountId}: {State}")]
    private static partial void LogStateSet(ILogger logger, string accountId, SyncAccountState state);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Checkpoint saved for account {AccountId}")]
    private static partial void LogCheckpointSaved(ILogger logger, string accountId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Checkpoint cleared for account {AccountId}")]
    private static partial void LogCheckpointCleared(ILogger logger, string accountId);
}
