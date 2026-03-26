using ReactiveUI;
using AStar.Dev.OneDriveSync.Theming;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly IThemeService _themeService;
    private ThemeOption _selectedThemeOption;

    public MainWindowViewModel(IThemeService themeService)
    {
        _themeService = themeService;

        ThemeOptions =
        [
            new ThemeOption(ThemeMode.Light, "Light"),
            new ThemeOption(ThemeMode.Dark,  "Dark"),
            new ThemeOption(ThemeMode.Auto,  "System")
        ];

        _selectedThemeOption = ThemeOptions.First(o => o.Mode == _themeService.CurrentMode);

        _ = this.WhenAnyValue(x => x.SelectedThemeOption)
                .Subscribe(opt => _themeService.Apply(opt.Mode));
    }

    public IReadOnlyList<ThemeOption> ThemeOptions { get; }

    public ThemeOption SelectedThemeOption
    {
        get => _selectedThemeOption;
        set => this.RaiseAndSetIfChanged(ref _selectedThemeOption, value);
    }
}
