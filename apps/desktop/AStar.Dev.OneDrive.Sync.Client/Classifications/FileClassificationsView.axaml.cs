using Avalonia;
using Avalonia.Controls;
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
        if (DataContext is not FileClassificationRulesViewModel vm || vm.IsLoaded)
            return;
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
}
