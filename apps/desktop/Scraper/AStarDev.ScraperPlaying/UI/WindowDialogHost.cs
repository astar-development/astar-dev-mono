using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The <see cref="IDialogHost"/> that anchors every dialog to a window.</summary>
/// <param name="owner">The window that owns the dialogs.</param>
public sealed class WindowDialogHost(Window owner) : IDialogHost
{
    private static readonly IReadOnlyList<FilePickerFileType> jsonFileTypes = [new FilePickerFileType("JSON configuration") { Patterns = ["*.json"] }];

    /// <inheritdoc/>
    public async Task<Option<string>> PickImportFileAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Import scrape configuration",
            FileTypeFilter = jsonFileTypes
        });

        return files.Count == 0 ? Option.None<string>() : Option.Some(files[0].Path.LocalPath);
    }

    /// <inheritdoc/>
    public async Task<Option<string>> PickExportFileAsync()
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export scrape configuration",
            SuggestedFileName = "scrape-configuration.json",
            DefaultExtension = "json",
            FileTypeChoices = jsonFileTypes
        });

        return file is null ? Option.None<string>() : Option.Some(file.Path.LocalPath);
    }

    /// <inheritdoc/>
    public async Task ShowAsync(Window dialog) => _ = await dialog.ShowDialog<bool>(owner);

    /// <inheritdoc/>
    public Task<bool> ConfirmAsync(string title, string message, string confirmLabel)
        => new ConfirmationWindow(title, message, confirmLabel).ShowDialog<bool>(owner);
}
