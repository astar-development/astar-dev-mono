using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Sync.Client.Search;

public partial class SyncedFileSearchView : UserControl
{
    public SyncedFileSearchView()
    {
        InitializeComponent();
        ResultsList.ElementPrepared += OnElementPrepared;
        ResultsList.ElementClearing += OnElementClearing;
    }

    private static void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element.DataContext is SyncedFileResultViewModel vm)
            _ = vm.LoadThumbnailAsync();
    }

    private static void OnElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element.DataContext is SyncedFileResultViewModel vm)
            vm.CancelThumbnailLoad();
    }
}
