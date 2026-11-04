using AStarDev.CloudSync.Core;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync;

/// <summary>Stands in for the sync engine until it exists: logs each requested action and does nothing else.</summary>
/// <param name="logger">Records each requested action.</param>
public sealed class PlaceholderSyncActions(ILogger<PlaceholderSyncActions> logger) : ISyncActions
{
    /// <inheritdoc />
    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken) => LogRequestAsync("Sync now requested (placeholder: the sync engine is not implemented yet)", accountId.ToString());

    /// <inheritdoc />
    public Task CancelSyncAsync(AccountId accountId, CancellationToken cancellationToken) => LogRequestAsync("Cancel sync requested (placeholder: the sync engine is not implemented yet)", accountId.ToString());

    /// <inheritdoc />
    public Task PauseAsync(AccountId accountId, CancellationToken cancellationToken) => LogRequestAsync("Pause requested (placeholder: the sync engine is not implemented yet)", accountId.ToString());

    /// <inheritdoc />
    public Task AddAccountAsync(CancellationToken cancellationToken) => LogRequestAsync("Add account requested (placeholder: the sync engine is not implemented yet)", string.Empty);

    /// <inheritdoc />
    public Task AddSyncPairAsync(Option<AccountId> accountId, CancellationToken cancellationToken) => LogRequestAsync("Add sync pair requested (placeholder: the sync engine is not implemented yet)", accountId.Match(id => id.ToString(), () => string.Empty));

    /// <inheritdoc />
    public Task KeepBothAsync(SyncConflict conflict, CancellationToken cancellationToken) => LogRequestAsync("Keep both requested (placeholder: the sync engine is not implemented yet)", conflict.SyncPairId.ToString());

    /// <inheritdoc />
    public Task KeepLocalAsync(SyncConflict conflict, CancellationToken cancellationToken) => LogRequestAsync("Keep local requested (placeholder: the sync engine is not implemented yet)", conflict.SyncPairId.ToString());

    /// <inheritdoc />
    public Task KeepCloudAsync(SyncConflict conflict, CancellationToken cancellationToken) => LogRequestAsync("Keep cloud requested (placeholder: the sync engine is not implemented yet)", conflict.SyncPairId.ToString());

    private Task LogRequestAsync(string action, string target)
    {
        LogMessage.Information(logger, nameof(PlaceholderSyncActions), action, target);

        return Task.CompletedTask;
    }
}
