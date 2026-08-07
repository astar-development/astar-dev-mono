using AStar.Dev.FunctionalParadigm;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Search;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class ApplicationInitializer(IStartupService startupService, IQuotaRefreshService quotaRefreshService, AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings, SyncedFileSearchViewModel search, ILogger<ApplicationInitializer> logger) : IApplicationInitializer
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            accounts.SubscribeToSyncEvents();
            activity.SubscribeToSyncEvents();
            dashboard.SubscribeToSyncEvents();
            dashboard.StartRefreshTimer();

            var restored = await startupService.RestoreAccountsAsync()
                .MatchAsync(
                    ok => ok,
                    error => throw new InvalidOperationException(error))
                .ConfigureAwait(false);

            accounts.RestoreAccounts(restored);

            foreach (var account in restored)
            {
                files.AddAccount(account);
                dashboard.AddAccount(account);
            }

            settings.LoadAccounts(restored);

            var activeAccount = restored.FirstOrDefault(account => account.IsActive);

            if (activeAccount is not null)
            {
                await files.ActivateAccountAsync(activeAccount.Id.Value).ConfigureAwait(false);
                await activity.SetActiveAccountAsync(activeAccount.Id.Value, activeAccount.Profile.Email).ConfigureAwait(false);
                search.SetActiveAccount(activeAccount.Id);
            }

            try
            {
                await RefreshQuotasAsync(restored, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OneDriveSyncClientMessages.QuotaRefreshStartupFailed(logger, ex.Message, ex);
            }
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.ApplicationInitializeFatal(logger, ex.Message, ex);
            throw;
        }
    }

    private async Task RefreshQuotasAsync(IReadOnlyList<OneDriveAccount> restored, CancellationToken cancellationToken)
    {
        foreach (var account in restored)
        {
            await quotaRefreshService.TryRefreshAsync(account, cancellationToken).ConfigureAwait(false);
            dashboard.UpdateQuota(account.Id.Value, account.Quota);
        }
    }
}
