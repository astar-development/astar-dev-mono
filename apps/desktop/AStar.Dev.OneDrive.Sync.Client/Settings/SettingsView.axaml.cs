using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AStar.Dev.OneDrive.Sync.Client.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void OnThemeLightClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is SettingsViewModel vm)
            vm.Theme = AppTheme.Light;
    }

    private void OnThemeDarkClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is SettingsViewModel vm)
            vm.Theme = AppTheme.Dark;
    }

    private void OnThemeSystemClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is SettingsViewModel vm)
            vm.Theme = AppTheme.System;
    }

    private void OnPolicyClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: ConflictPolicy policy } && DataContext is SettingsViewModel vm)
        {
            vm.DefaultConflictPolicy = policy;
        }
    }

    private void OnIntervalClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: int minutes } && DataContext is SettingsViewModel vm)
        {
            vm.SyncIntervalMinutes = minutes;
        }
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        if(sender is not Button { Tag: string accountId })
            return;
        if(DataContext is not SettingsViewModel vm)
            return;

        var account = vm.AccountSettings.FirstOrDefault(a => a.AccountId == accountId);
        if(account is null)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if(topLevel is null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title         = "Choose local sync folder",
                AllowMultiple = false
            });

        if(folders is [{ } folder])
            account.LocalSyncPath = folder.Path.LocalPath;
    }
}
