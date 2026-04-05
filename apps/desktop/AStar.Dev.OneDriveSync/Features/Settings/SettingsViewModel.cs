using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Dashboard;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Settings;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IUserTypeService    _userTypeService;
    private readonly ILocalisationService _localisationService;
    private ThemeMode _selectedTheme;
    private string    _selectedLocale;
    private UserType  _selectedUserType;
    private bool      _notificationsEnabled;

    public SettingsViewModel(IThemeService themeService, ILocalisationService localisationService, IUserTypeService userTypeService, IDialogService dialogService, INotificationsService notificationsService)
    {
        _userTypeService      = userTypeService;
        _localisationService  = localisationService;

        ThemeModes            = new ReadOnlyCollection<ThemeMode>([ThemeMode.Auto, ThemeMode.Light, ThemeMode.Dark]);
        UserTypes             = new ReadOnlyCollection<UserType>([UserType.Casual, UserType.PowerUser]);
        _selectedTheme        = themeService.CurrentMode;
        _selectedLocale       = localisationService.CurrentLocale;
        _selectedUserType     = userTypeService.CurrentUserType;
        _notificationsEnabled = notificationsService.NotificationsEnabled;

        ChangeThemeCommand = ReactiveCommand.CreateFromTask<ThemeMode, Result<ThemeMode, ErrorResponse>>(
            (mode, ct) => themeService.SetThemeAsync(mode, ct));

        ChangeLocaleCommand = ReactiveCommand.CreateFromTask<string, Result<string, ErrorResponse>>(
            (locale, ct) => localisationService.SetLocaleAsync(locale, ct));

        SetNotificationsCommand = ReactiveCommand.CreateFromTask<bool, Result<bool, ErrorResponse>>(
            (enabled, ct) => notificationsService.SetEnabledAsync(enabled, ct));

        _ = this.WhenAnyValue(vm => vm.SelectedTheme)
            .Skip(1)
            .InvokeCommand(ChangeThemeCommand);

        _ = this.WhenAnyValue(vm => vm.SelectedLocale)
            .Skip(1)
            .InvokeCommand(ChangeLocaleCommand);

        _ = this.WhenAnyValue(vm => vm.SelectedUserType)
            .Skip(1)
            .Subscribe(userType => userTypeService.RequestUserTypeChange(userType));

        _ = this.WhenAnyValue(vm => vm.NotificationsEnabled)
            .Skip(1)
            .InvokeCommand(SetNotificationsCommand);

        SupportedLocales = localisationService.SupportedLocales;

        // async void: Avalonia event handler — acceptable exception per checklist
        // ReSharper disable once AsyncVoidLambda
        userTypeService.ConfirmationRequested += async (_, _) =>
        {
            string title   = _localisationService.GetString(SettingsStrings.PowerUserConfirmTitle);
            string message = _localisationService.GetString(SettingsStrings.PowerUserConfirmMessage);

            bool confirmed = await dialogService.ConfirmAsync(title, message, CancellationToken.None).ConfigureAwait(true);

            userTypeService.ConfirmUserTypeChange(confirmed);

            if (!confirmed)
            {
                _selectedUserType = _userTypeService.CurrentUserType;
                this.RaisePropertyChanged(nameof(SelectedUserType));
            }
        };
    }

    /// <summary>Available theme modes for selection.</summary>
    public ReadOnlyCollection<ThemeMode> ThemeModes { get; }

    /// <summary>Available user types for selection.</summary>
    public ReadOnlyCollection<UserType> UserTypes { get; }

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

    /// <summary>The currently selected user type; switching to Power User requires confirmation.</summary>
    public UserType SelectedUserType
    {
        get => _selectedUserType;
        set => this.RaiseAndSetIfChanged(ref _selectedUserType, value);
    }

    /// <summary>Whether OS notifications are enabled; persisted on change.</summary>
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => this.RaiseAndSetIfChanged(ref _notificationsEnabled, value);
    }

    /// <summary>Persists and applies the selected theme; exposed for testing.</summary>
    public ReactiveCommand<ThemeMode, Result<ThemeMode, ErrorResponse>> ChangeThemeCommand { get; }

    /// <summary>Persists and applies the selected locale; exposed for testing.</summary>
    public ReactiveCommand<string, Result<string, ErrorResponse>> ChangeLocaleCommand { get; }

    /// <summary>Persists the notification preference; exposed for testing.</summary>
    public ReactiveCommand<bool, Result<bool, ErrorResponse>> SetNotificationsCommand { get; }
}
