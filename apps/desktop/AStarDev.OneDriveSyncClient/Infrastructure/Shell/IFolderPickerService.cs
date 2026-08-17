using Avalonia.Platform.Storage;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <summary>Abstracts platform-level folder picking to keep consumers testable without Avalonia infrastructure.</summary>
public interface IFolderPickerService
{
    /// <summary>Opens a folder picker dialog and returns the selected folder's local path, or <c>null</c> if the user cancelled.</summary>
    Task<string?> PickFolderAsync(IStorageProvider storageProvider, string title, CancellationToken cancellationToken = default);
}
