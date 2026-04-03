using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Settings;

public sealed class SettingsViewModel : ViewModelBase
{
    private ThemeMode _selectedTheme;
    private string _selectedLocale;

    public SettingsViewModel(IThemeService themeService, ILocalisationService localisationService)
    {
        ThemeModes    = new ReadOnlyCollection<ThemeMode>([ThemeMode.Auto, ThemeMode.Light, ThemeMode.Dark]);
        _selectedTheme  = themeService.CurrentMode;
        _selectedLocale = localisationService.CurrentLocale;

        ChangeThemeCommand = ReactiveCommand.CreateFromTask<ThemeMode, Result<ThemeMode, ErrorResponse>>(
            (mode, ct) => themeService.SetThemeAsync(mode, ct));

        ChangeLocaleCommand = ReactiveCommand.CreateFromTask<string, Result<string, ErrorResponse>>(
            (locale, ct) => localisationService.SetLocaleAsync(locale, ct));

        _ = this.WhenAnyValue(vm => vm.SelectedTheme)
            .Skip(1)
            .InvokeCommand(ChangeThemeCommand);

        _ = this.WhenAnyValue(vm => vm.SelectedLocale)
            .Skip(1)
            .InvokeCommand(ChangeLocaleCommand);

        SupportedLocales = localisationService.SupportedLocales;
    }

    /// <summary>Available theme modes for selection.</summary>
    public ReadOnlyCollection<ThemeMode> ThemeModes { get; }

    /// <summary>Supported locale codes exposed for the locale selector dropdown.</summary>
    public IReadOnlySet<string> SupportedLocales { get; }

    /// <summary>The currently selected theme mode; changing it immediately applies and persists the theme.</summary>
    public ThemeMode SelectedTheme
    {
        get => _selectedTheme;
        set => this.RaiseAndSetIfChanged(ref _selectedTheme, value);
    }

    /// <summary>The currently selected locale; changing it immediately applies and persists the locale.</summary>
    public string SelectedLocale
    {
        get => _selectedLocale;
        set => this.RaiseAndSetIfChanged(ref _selectedLocale, value);
    }

    /// <summary>Persists and applies the selected theme; exposed for testing.</summary>
    public ReactiveCommand<ThemeMode, Result<ThemeMode, ErrorResponse>> ChangeThemeCommand { get; }

    /// <summary>Persists and applies the selected locale; exposed for testing.</summary>
    public ReactiveCommand<string, Result<string, ErrorResponse>> ChangeLocaleCommand { get; }
}
