using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void OnThemeClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: AppTheme theme } && DataContext is SettingsViewModel vm)
            vm.Theme = theme;
    }

    private void OnLanguageClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: CultureInfo culture } && DataContext is SettingsViewModel vm)
            _ = vm.SelectCultureAsync(culture);
    }

    private void OnPolicyClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: ConflictPolicy policy } && DataContext is SettingsViewModel vm)
            vm.DefaultConflictPolicy = policy;
    }

    private void OnIntervalClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: int minutes } && DataContext is SettingsViewModel vm)
            vm.SyncIntervalMinutes = minutes;
    }

    private void OnWorkerCountClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: int count } && DataContext is SettingsViewModel vm)
            vm.ConcurrentWorkerCount = count;
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        if(sender is not Button { Tag: string accountId }) return;
        if(DataContext is not SettingsViewModel vm) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if(topLevel is null) return;
        await vm.BrowseForAccountFolderAsync(accountId, topLevel.StorageProvider);
    }
}
