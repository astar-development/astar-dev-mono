using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class ApplicationInitializer(IStartupService startupService, IQuotaRefreshService quotaRefreshService, AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings, ILogger<ApplicationInitializer> logger) : IApplicationInitializer
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            accounts.SubscribeToSyncEvents();
            activity.SubscribeToSyncEvents();
            dashboard.SubscribeToSyncEvents();

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
                await files.ActivateAccountAsync(activeAccount.Id.Id).ConfigureAwait(false);
                await activity.SetActiveAccountAsync(activeAccount.Id.Id, activeAccount.Profile.Email).ConfigureAwait(false);
            }

            try
            {
                await RefreshQuotasAsync(restored, ct).ConfigureAwait(false);
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

    private async Task RefreshQuotasAsync(IReadOnlyList<OneDriveAccount> restored, CancellationToken ct)
    {
        foreach (var account in restored)
        {
            await quotaRefreshService.TryRefreshAsync(account, ct).ConfigureAwait(false);
            dashboard.UpdateQuota(account.Id.Id, account.Quota);
        }
    }
}
