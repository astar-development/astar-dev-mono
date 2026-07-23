namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <inheritdoc />
public sealed class UpdateNotificationService(IVelopackUpdateService updateCheckService, IUpdateAvailableViewModelFactory viewModelFactory, IUpdateAvailableDialogService dialogService) : IUpdateNotificationService
{
    /// <inheritdoc />
    public async Task CheckAndNotifyAsync(CancellationToken cancellationToken = default)
    {
        var updateInfo = await updateCheckService.CheckForUpdatesAsync(cancellationToken).ConfigureAwait(false);
        if (updateInfo is null)
            return;

        var viewModel = viewModelFactory.Create(updateInfo);

        try
        {
            await dialogService.ShowAsync(viewModel, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            viewModel.Dispose();
        }
    }
}
