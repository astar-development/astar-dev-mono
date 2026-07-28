using AStar.Dev.Clock.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.Clock;

/// <summary>The main window's view model, exposing the theme selection presented in the File menu.</summary>
/// <param name="themeService">Applies and persists the selected <see cref="ThemeMode" />.</param>
public sealed partial class MainWindowViewModel(IThemeService themeService) : ObservableObject
{
    private ThemeMode selectedTheme = themeService.CurrentTheme;

    /// <summary>Gets a value indicating whether <see cref="ThemeMode.System" /> is the currently selected theme.</summary>
    public bool IsSystemThemeSelected => selectedTheme == ThemeMode.System;

    /// <summary>Gets a value indicating whether <see cref="ThemeMode.Light" /> is the currently selected theme.</summary>
    public bool IsLightThemeSelected => selectedTheme == ThemeMode.Light;

    /// <summary>Gets a value indicating whether <see cref="ThemeMode.Dark" /> is the currently selected theme.</summary>
    public bool IsDarkThemeSelected => selectedTheme == ThemeMode.Dark;

    [RelayCommand]
    private void SelectTheme(ThemeMode themeMode)
    {
        themeService.ApplyTheme(themeMode);
        selectedTheme = themeMode;
        OnPropertyChanged(nameof(IsSystemThemeSelected));
        OnPropertyChanged(nameof(IsLightThemeSelected));
        OnPropertyChanged(nameof(IsDarkThemeSelected));
    }
}
