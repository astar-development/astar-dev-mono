using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AStarDev.CloudSync.Common;

/// <summary>Shows the platform's folder chooser over the main window.</summary>
/// <param name="fileSystem">Creates the folder abstraction for the chosen path.</param>
/// <param name="localization">Supplies the chooser's title.</param>
[ExcludeFromCodeCoverage(Justification = "Opens the operating system's folder chooser, which cannot run in automated tests.")]
public sealed class AvaloniaFolderPicker(IFileSystem fileSystem, ILocalizationService localization) : IFolderPicker
{
    /// <inheritdoc />
    public async Task<Option<IDirectoryInfo>> PickAsync(Option<IDirectoryInfo> startIn, CancellationToken cancellationToken)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return Option.None<IDirectoryInfo>();

        var startFolder = await startIn.MatchAsync(folder => window.StorageProvider.TryGetFolderFromPathAsync(folder.FullName), () => Task.FromResult<IStorageFolder?>(null));
        var chosen = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, Title = localization.GetLocal("AccountDialog.SyncFolder.PickerTitle"), SuggestedStartLocation = startFolder });
        cancellationToken.ThrowIfCancellationRequested();

        return chosen.Select(folder => folder.TryGetLocalPath()).FirstOrDefault(path => !string.IsNullOrWhiteSpace(path)) is { } localPath
            ? Option.Some(fileSystem.DirectoryInfo.New(localPath))
            : Option.None<IDirectoryInfo>();
    }
}
