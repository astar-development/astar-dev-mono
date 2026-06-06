using Avalonia.Platform.Storage;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class AvaloniaFilePickerService : IFilePickerService
{
    /// <inheritdoc />
    public async Task<string?> PickSaveFileAsync(IStorageProvider storageProvider, string title, string suggestedName, string extensionFilter, CancellationToken ct = default)
    {
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            FileTypeChoices = [new FilePickerFileType(extensionFilter.ToUpperInvariant()) { Patterns = [$"*.{extensionFilter}"] }]
        }).ConfigureAwait(false);

        return file?.Path?.LocalPath;
    }

    /// <inheritdoc />
    public async Task<string?> PickOpenFileAsync(IStorageProvider storageProvider, string title, string extensionFilter, CancellationToken ct = default)
    {
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(extensionFilter.ToUpperInvariant()) { Patterns = [$"*.{extensionFilter}"] }]
        }).ConfigureAwait(false);

        return files is [{ } file] ? file.Path?.LocalPath : null;
    }
}
