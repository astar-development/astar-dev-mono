using System.ComponentModel;
using AStarDev.OneDriveSyncClient.Classifications;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class AvaloniaCategoryEditDialogService : ICategoryEditDialogService
{
    /// <inheritdoc />
    public async Task ShowAsync(CategoryNodeViewModel node, CancellationToken cancellationToken = default)
        => await Dispatcher.UIThread.InvokeAsync(() => ShowDialogAsync(node, cancellationToken));

    private static async Task ShowDialogAsync(CategoryNodeViewModel node, CancellationToken cancellationToken)
    {
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow is null)
            return;

        var dialog = new CategoryEditDialog { DataContext = node };

        void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoryNodeViewModel.IsEditing) && !node.IsEditing)
                dialog.Close();
        }

        node.PropertyChanged += OnNodePropertyChanged;
        using var registration = cancellationToken.Register(dialog.Close);

        try
        {
            await dialog.ShowDialog(mainWindow).ConfigureAwait(false);
        }
        finally
        {
            node.PropertyChanged -= OnNodePropertyChanged;
        }
    }
}
