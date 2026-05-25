using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class ApplicationInitializer(IStartupService startupService, AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings, ILogger<ApplicationInitializer> logger) : IApplicationInitializer
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            accounts.SubscribeToSyncEvents();
            activity.SubscribeToSyncEvents();
            dashboard.SubscribeToSyncEvents();

            var restored = await startupService.RestoreAccountsAsync().ConfigureAwait(false);

            accounts.RestoreAccounts(restored);

            foreach (var account in restored)
            {
                files.AddAccount(account);
                dashboard.AddAccount(account);
            }

            settings.LoadAccounts(restored);
            await settings.ClassificationRules.LoadAsync(ct).ConfigureAwait(false);

            var activeAccount = restored.FirstOrDefault(account => account.IsActive);

            if (activeAccount is not null)
            {
                await files.ActivateAccountAsync(activeAccount.Id.Id).ConfigureAwait(false);
                await activity.SetActiveAccountAsync(activeAccount.Id.Id, activeAccount.Profile.Email).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.ApplicationInitializeFatal(logger, ex.Message, ex);
            throw;
        }
    }
}
