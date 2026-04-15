using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class ApplicationInitializer(IStartupService startupService, AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings) : IApplicationInitializer
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

            foreach(var account in restored)
            {
                files.AddAccount(account);
                dashboard.AddAccount(account);
            }

            settings.LoadAccounts(restored);

            var activeAccount = restored.FirstOrDefault(account => account.IsActive);

            if(activeAccount is not null)
            {
                await files.ActivateAccountAsync(activeAccount.Id.Id).ConfigureAwait(false);
                await activity.SetActiveAccountAsync(activeAccount.Id.Id, activeAccount.Email).ConfigureAwait(false);
            }
        }
        catch(Exception ex)
        {
            Serilog.Log.Fatal(ex, "[ApplicationInitializer.InitializeAsync] FATAL ERROR: {Error}", ex.Message);
            throw;
        }
    }
}
