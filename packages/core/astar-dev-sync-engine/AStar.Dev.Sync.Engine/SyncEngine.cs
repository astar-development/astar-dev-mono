using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Default sync engine implementation that orchestrates bi-directional sync (SE-01)
/// with configurable concurrency (SE-02) and per-account locking (SE-08).
/// </summary>
public sealed partial class SyncEngine(ISyncProvider syncProvider, ISyncLock syncLock, IReadOnlyDictionary<string, AccountSyncOptions> accountOptions, SyncOptions options, ILogger<SyncEngine> logger) : ISyncEngine
{
    /// <inheritdoc />
    public async Task<Result<SyncReport, ErrorResponse>> SyncAsync(string accountId, CancellationToken ct = default)
    {
        if (!accountOptions.TryGetValue(accountId, out var account))
        {
            return new Result<SyncReport, ErrorResponse>.Error(new ErrorResponse($"No configuration found for account '{accountId}'."));
        }

        if (!syncLock.TryAcquire(accountId))
        {
            return new Result<SyncReport, ErrorResponse>.Error(new ErrorResponse($"A sync is already running for account '{accountId}'."));
        }

        try
        {
            var startedAt = DateTimeOffset.UtcNow;
            LogSyncStarted(logger, accountId);

            var changes = await syncProvider.GetChangesAsync(accountId, account.SelectedFolders, ct).ConfigureAwait(false);

            if (changes.Count == 0)
            {
                LogNoChanges(logger, accountId);

                return new Result<SyncReport, ErrorResponse>.Ok(new SyncReport { AccountId = accountId, StartedAtUtc = startedAt, CompletedAtUtc = DateTimeOffset.UtcNow, ItemResults = [] });
            }

            var maxConcurrency = account.MaxConcurrency ?? options.MaxConcurrency;
            LogProcessingItems(logger, changes.Count, accountId, maxConcurrency);

            var results = await TransferAsync(accountId, changes, maxConcurrency, ct).ConfigureAwait(false);

            var report = new SyncReport { AccountId = accountId, StartedAtUtc = startedAt, CompletedAtUtc = DateTimeOffset.UtcNow, ItemResults = results };

            LogSyncCompleted(logger, accountId, report.Uploaded, report.Downloaded, report.Failed);

            return new Result<SyncReport, ErrorResponse>.Ok(report);
        }
        catch (OperationCanceledException)
        {
            LogSyncCancelled(logger, accountId);

            return new Result<SyncReport, ErrorResponse>.Error(new ErrorResponse($"Sync cancelled for account '{accountId}'."));
        }
        catch (InvalidOperationException ex)
        {
            LogSyncFailed(logger, ex, accountId);

            return new Result<SyncReport, ErrorResponse>.Error(new ErrorResponse($"Sync failed for account '{accountId}': {ex.Message}"));
        }
        catch (IOException ex)
        {
            LogSyncFailed(logger, ex, accountId);

            return new Result<SyncReport, ErrorResponse>.Error(new ErrorResponse($"Sync failed for account '{accountId}': {ex.Message}"));
        }
        catch (HttpRequestException ex)
        {
            LogSyncFailed(logger, ex, accountId);

            return new Result<SyncReport, ErrorResponse>.Error(new ErrorResponse($"Sync failed for account '{accountId}': {ex.Message}"));
        }
        finally
        {
            syncLock.Release(accountId);
        }
    }

    private async Task<IReadOnlyList<SyncItemResult>> TransferAsync(string accountId, IReadOnlyList<SyncItem> items, int maxConcurrency, CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                return item.Direction switch
                {
                    SyncDirection.LocalToRemote => await syncProvider.UploadAsync(accountId, item, ct).ConfigureAwait(false),
                    SyncDirection.RemoteToLocal => await syncProvider.DownloadAsync(accountId, item, ct).ConfigureAwait(false),
                    _ => new SyncItemResult { Item = item, Succeeded = false, ErrorMessage = $"Unknown direction: {item.Direction}" }
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogTransferFailed(logger, ex, item.RelativePath, item.Direction);

                return new SyncItemResult { Item = item, Succeeded = false, ErrorMessage = ex.Message };
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync started for account {AccountId}.")]
    private static partial void LogSyncStarted(ILogger logger, string accountId);

    [LoggerMessage(Level = LogLevel.Information, Message = "No changes detected for account {AccountId}.")]
    private static partial void LogNoChanges(ILogger logger, string accountId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Count} items for account {AccountId} with concurrency {Concurrency}.")]
    private static partial void LogProcessingItems(ILogger logger, int count, string accountId, int concurrency);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync completed for account {AccountId}: {Uploaded} uploaded, {Downloaded} downloaded, {Failed} failed.")]
    private static partial void LogSyncCompleted(ILogger logger, string accountId, int uploaded, int downloaded, int failed);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sync cancelled for account {AccountId}.")]
    private static partial void LogSyncCancelled(ILogger logger, string accountId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sync failed for account {AccountId}.")]
    private static partial void LogSyncFailed(ILogger logger, Exception ex, string accountId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transfer failed for {Path} ({Direction}).")]
    private static partial void LogTransferFailed(ILogger logger, Exception ex, string path, SyncDirection direction);
}
