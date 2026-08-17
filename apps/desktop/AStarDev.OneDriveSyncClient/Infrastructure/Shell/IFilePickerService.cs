using Avalonia.Platform.Storage;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <summary>Abstracts platform-level file picking to keep consumers testable without Avalonia infrastructure.</summary>
public interface IFilePickerService
{
    /// <summary>Opens a save-file dialog and returns the selected path, or <c>null</c> if the user cancelled.</summary>
    Task<string?> PickSaveFileAsync(IStorageProvider storageProvider, string title, string suggestedName, string extensionFilter, CancellationToken cancellationToken = default);

    /// <summary>Opens an open-file dialog and returns the selected path, or <c>null</c> if the user cancelled.</summary>
    Task<string?> PickOpenFileAsync(IStorageProvider storageProvider, string title, string extensionFilter, CancellationToken cancellationToken = default);
}
