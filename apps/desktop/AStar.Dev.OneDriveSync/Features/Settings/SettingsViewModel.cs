using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Settings;

public sealed class SettingsViewModel : ViewModelBase
{
    private ThemeMode _selectedTheme;

    public SettingsViewModel(IThemeService themeService)
    {
        ThemeModes = new ReadOnlyCollection<ThemeMode>([ThemeMode.Auto, ThemeMode.Light, ThemeMode.Dark]);
        _selectedTheme = themeService.CurrentMode;

        ChangeThemeCommand = ReactiveCommand.CreateFromTask<ThemeMode, Result<ThemeMode, ErrorResponse>>(
            (mode, ct) => themeService.SetThemeAsync(mode, ct));

        _ = this.WhenAnyValue(vm => vm.SelectedTheme)
            .Skip(1)
            .InvokeCommand(ChangeThemeCommand);
    }

    /// <summary>Available theme modes for selection.</summary>
    public ReadOnlyCollection<ThemeMode> ThemeModes { get; }

    /// <summary>The currently selected theme mode; changing it immediately applies and persists the theme.</summary>
    public ThemeMode SelectedTheme
    {
        get => _selectedTheme;
        set => this.RaiseAndSetIfChanged(ref _selectedTheme, value);
    }

    /// <summary>Persists and applies the selected theme; exposed for testing.</summary>
    public ReactiveCommand<ThemeMode, Result<ThemeMode, ErrorResponse>> ChangeThemeCommand { get; }
}
