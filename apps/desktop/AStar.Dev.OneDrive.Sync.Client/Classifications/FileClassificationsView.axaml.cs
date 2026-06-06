using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

public partial class FileClassificationsView : UserControl
{
    public FileClassificationsView() => InitializeComponent();

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
