using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

public partial class FileClassificationsView : UserControl, IDisposable
{
    private CancellationTokenSource? cts;

    public FileClassificationsView() => InitializeComponent();

    public void Dispose()
    {
        cts?.Dispose();
        cts = null;
        GC.SuppressFinalize(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is not FileClassificationRulesViewModel vm)
            return;
        vm.PrepareForLoad();
        cts = new CancellationTokenSource();
        Dispatcher.UIThread.InvokeAsync(() => vm.LoadAsync(cts.Token), DispatcherPriority.Background);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

#pragma warning disable IDE1006 // Naming Styles - this does not apply to event handlers
    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FileClassificationRulesViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        await vm.ExportAsync(topLevel.StorageProvider);
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FileClassificationRulesViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        await vm.ImportAsync(topLevel.StorageProvider);
    }
#pragma warning restore IDE1006 // Naming Styles

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.PageDown:
                CategoriesScrollViewer.PageDown();
                e.Handled = true;

                break;
            case Key.PageUp:
                CategoriesScrollViewer.PageUp();
                e.Handled = true;

                break;
            case Key.End:
                CategoriesScrollViewer.ScrollToEnd();
                e.Handled = true;

                break;
            case Key.Home:
                CategoriesScrollViewer.ScrollToHome();
                e.Handled = true;

                break;
        }
    }
}
