using Avalonia.Platform.Storage;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class AvaloniaFolderPickerService : IFolderPickerService
{
    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(IStorageProvider storageProvider, string title, CancellationToken ct = default)
    {
        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false }).ConfigureAwait(false);

        return folders is [{ } folder] ? folder.Path?.LocalPath : null;
    }
}
