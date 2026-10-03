using System.IO.Abstractions;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Edits the scrape directories of a scrape configuration and warns, without blocking, about directories that do not exist.</summary>
public sealed partial class DirectorySettingsEditor : UserControl
{
    private IFileSystem? fileSystem;

    public DirectorySettingsEditor() => InitializeComponent();

    /// <summary>Fills the controls from the specified settings.</summary>
    /// <param name="input">The settings to display.</param>
    /// <param name="fileSystem">The file system used to check that the directories exist.</param>
    public void Load(DirectorySettingsInput input, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
        RootDirectoryBox.Text = input.RootDirectory;
        RootDirectoryFamousBox.Text = input.RootDirectoryFamous;
        SubDirectoryNameBox.Text = input.SubDirectoryName;
        RefreshMissingDirectories();
    }

    /// <summary>Reads the settings currently entered in the controls.</summary>
    public DirectorySettingsInput ReadInput() => new(
        RootDirectoryBox.Text ?? string.Empty,
        RootDirectoryFamousBox.Text ?? string.Empty,
        SubDirectoryNameBox.Text ?? string.Empty);

    public void DirectoryChanged(object? sender, TextChangedEventArgs eventArgs) => RefreshMissingDirectories();

    public async void BrowseRootDirectory(object? sender, RoutedEventArgs eventArgs) =>
        await BrowseAsync(RootDirectoryBox, "Select the root directory");

    public async void BrowseRootDirectoryFamous(object? sender, RoutedEventArgs eventArgs) =>
        await BrowseAsync(RootDirectoryFamousBox, "Select the famous root directory");

    private async Task BrowseAsync(TextBox target, string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        if (folders.Count > 0) target.Text = folders[0].Path.LocalPath;
    }

    private void RefreshMissingDirectories()
    {
        if (fileSystem is null) return;

        MissingDirectoriesText.Text = string.Join(Environment.NewLine, ReadInput().MissingDirectories(fileSystem));
    }
}
