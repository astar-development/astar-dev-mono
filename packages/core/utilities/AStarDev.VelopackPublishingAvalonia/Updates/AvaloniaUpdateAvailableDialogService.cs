using global::Avalonia;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Threading;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <inheritdoc />
public sealed class AvaloniaUpdateAvailableDialogService : IUpdateAvailableDialogService
{
    /// <inheritdoc />
    public async Task ShowAsync(UpdateAvailableViewModel viewModel, CancellationToken cancellationToken = default)
        => await Dispatcher.UIThread.InvokeAsync(() => ShowDialogAsync(viewModel, cancellationToken)).ConfigureAwait(false);

    private static async Task ShowDialogAsync(UpdateAvailableViewModel viewModel, CancellationToken cancellationToken)
    {
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow is null)
            return;

        var dialog = new UpdateAvailableView { DataContext = viewModel };

        void OnCloseRequested(object? sender, EventArgs args) => dialog.Close();
        viewModel.CloseRequested += OnCloseRequested;

        using var registration = cancellationToken.Register(dialog.Close);

        try
        {
            await dialog.ShowDialog(mainWindow).ConfigureAwait(false);
        }
        finally
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }
    }
}
