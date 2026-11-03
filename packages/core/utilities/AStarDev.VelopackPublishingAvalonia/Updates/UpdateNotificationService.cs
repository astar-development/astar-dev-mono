using global::Velopack;

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

        await ShowAsync(updateInfo, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckNowAsync(CancellationToken cancellationToken = default)
    {
        var result = await updateCheckService.CheckForUpdatesWithResultAsync(cancellationToken).ConfigureAwait(false);
        if (result is UpdateFound found)
            await ShowAsync(found.Info, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async Task ShowAsync(UpdateInfo updateInfo, CancellationToken cancellationToken)
    {
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
