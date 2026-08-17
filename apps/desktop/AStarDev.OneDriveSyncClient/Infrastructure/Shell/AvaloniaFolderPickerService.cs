using Avalonia.Platform.Storage;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class AvaloniaFolderPickerService : IFolderPickerService
{
    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(IStorageProvider storageProvider, string title, CancellationToken cancellationToken = default)
    {
        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false }).ConfigureAwait(false);

        return folders is [{ } folder] ? folder.Path?.LocalPath : null;
    }
}
