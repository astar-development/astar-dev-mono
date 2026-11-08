using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Core.Common;
using AStarDev.CloudSync.Core.Conflicts;
using AStarDev.CloudSync.Core.SyncPairs;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync;

/// <summary>Stands in for the sync engine until it exists: opens the add-account dialog for "add account", and logs every other requested action without doing anything else.</summary>
/// <param name="addAccountDialog">Shows the add-account dialog.</param>
/// <param name="logger">Records each requested placeholder action.</param>
public sealed class PlaceholderSyncActions(IAddAccountDialog addAccountDialog, ILogger<PlaceholderSyncActions> logger) : ISyncActions
{
    /// <inheritdoc />
    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken) => LogRequestAsync("Sync now requested (placeholder: the sync engine is not implemented yet)", accountId.ToString());

    /// <inheritdoc />
    public Task CancelSyncAsync(AccountId accountId, CancellationToken cancellationToken) => LogRequestAsync("Cancel sync requested (placeholder: the sync engine is not implemented yet)", accountId.ToString());

    /// <inheritdoc />
    public Task PauseAsync(AccountId accountId, CancellationToken cancellationToken) => LogRequestAsync("Pause requested (placeholder: the sync engine is not implemented yet)", accountId.ToString());

    /// <inheritdoc />
    public Task AddAccountAsync(CancellationToken cancellationToken) => addAccountDialog.ShowAsync(cancellationToken);

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
