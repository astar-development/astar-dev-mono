using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AStar.Dev.File.App.Services;

public class FolderPickerService : IFolderPickerService
{
    public async Task<string?> OpenFolderPickerAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var mainWindow = desktop.MainWindow;
        if (mainWindow is null)
            return null;

        var topLevel = TopLevel.GetTopLevel(mainWindow);
        if (topLevel is null)
            return null;

        var results = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Root Folder",
                AllowMultiple = false
            });

        return results.Count>=0 ? results[0].Path.LocalPath : null;
    }
}
