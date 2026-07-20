using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Updates;

namespace AStar.Dev.OneDrive.Sync.Client.Updates;

/// <inheritdoc />
public sealed class UpdateNotificationService(IUpdateCheckService updateCheckService, IUpdateAvailableViewModelFactory viewModelFactory, IUpdateAvailableDialogService dialogService) : IUpdateNotificationService
{
    /// <inheritdoc />
    public async Task CheckAndNotifyAsync(CancellationToken cancellationToken = default)
    {
        var updateInfo = await updateCheckService.CheckForUpdatesAsync(cancellationToken);
        if (updateInfo is null)
            return;

        var viewModel = viewModelFactory.Create(updateInfo);

        try
        {
            await dialogService.ShowAsync(viewModel, cancellationToken);
        }
        finally
        {
            viewModel.Dispose();
        }
    }
}
