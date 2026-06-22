using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Sync.Client.Search;

public partial class SyncedFileSearchView : UserControl
{
    public SyncedFileSearchView()
    {
        InitializeComponent();
        ResultsList.ContainerPrepared += OnContainerPrepared;
        ResultsList.ContainerClearing += OnContainerClearing;
    }

    private static void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container.DataContext is SyncedFileResultViewModel vm)
            _ = vm.LoadThumbnailAsync();
    }

    private static void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
    {
        if (e.Container.DataContext is SyncedFileResultViewModel vm)
            vm.CancelThumbnailLoad();
    }
}
