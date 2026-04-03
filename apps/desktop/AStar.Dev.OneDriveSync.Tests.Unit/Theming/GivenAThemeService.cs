using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReactiveUI;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Theming;

public sealed class GivenAThemeService
{
    private readonly IApplicationThemeAdapter  _themeAdapter       = Substitute.For<IApplicationThemeAdapter>();
    private readonly IPlatformThemeProvider    _platformProvider   = Substitute.For<IPlatformThemeProvider>();
    private readonly IAppSettingsRepository    _settingsRepository = Substitute.For<IAppSettingsRepository>();
    private readonly ILogger<ThemeService>     _logger             = Substitute.For<ILogger<ThemeService>>();

    public GivenAThemeService()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _ = _settingsRepository.SaveAsync(Arg.Any<AppSettings>(), Arg.Any<CancellationToken>())
            .Returns(new Result<AppSettings, ErrorResponse>.Ok(new AppSettings()));

        _ = _platformProvider.DarkModeChanged.Returns(System.Reactive.Linq.Observable.Never<bool>());
    }

    [Fact]
    public async Task when_set_theme_dark_then_theme_changed_observable_emits_dark()
    {
        ThemeMode? emitted = null;
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);
        _ = sut.ThemeChanged.Subscribe(m => emitted = m);

        _ = await sut.SetThemeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);

        emitted.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public async Task when_set_theme_light_then_theme_changed_observable_emits_light()
    {
        ThemeMode? emitted = null;
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);
        _ = sut.ThemeChanged.Subscribe(m => emitted = m);

        _ = await sut.SetThemeAsync(ThemeMode.Light, TestContext.Current.CancellationToken);

        emitted.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public async Task when_set_theme_dark_then_adapter_receives_dark_variant()
    {
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.SetThemeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);

        _themeAdapter.Received(1).Apply(ThemeVariant.Dark);
    }

    [Fact]
    public async Task when_set_theme_light_then_adapter_receives_light_variant()
    {
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.SetThemeAsync(ThemeMode.Light, TestContext.Current.CancellationToken);

        _themeAdapter.Received(1).Apply(ThemeVariant.Light);
    }

    [Fact]
    public async Task when_set_theme_auto_then_adapter_reflects_current_platform_dark_state()
    {
        var darkModeSubject = new Subject<bool>();
        _ = _platformProvider.IsDarkMode.Returns(true);
        _ = _platformProvider.DarkModeChanged.Returns(darkModeSubject);

        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);
        _ = await sut.SetThemeAsync(ThemeMode.Auto, TestContext.Current.CancellationToken);

        _themeAdapter.Received().Apply(ThemeVariant.Dark);
    }

    [Fact]
    public async Task when_set_theme_auto_then_platform_dark_change_triggers_dark_variant()
    {
        var darkModeSubject = new Subject<bool>();
        _ = _platformProvider.IsDarkMode.Returns(false);
        _ = _platformProvider.DarkModeChanged.Returns(darkModeSubject);

        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);
        _ = await sut.SetThemeAsync(ThemeMode.Auto, TestContext.Current.CancellationToken);
        _themeAdapter.ClearReceivedCalls();

        darkModeSubject.OnNext(true);

        _themeAdapter.Received(1).Apply(ThemeVariant.Dark);
    }

    [Fact]
    public async Task when_set_theme_auto_then_platform_light_change_triggers_light_variant()
    {
        var darkModeSubject = new Subject<bool>();
        _ = _platformProvider.IsDarkMode.Returns(true);
        _ = _platformProvider.DarkModeChanged.Returns(darkModeSubject);

        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);
        _ = await sut.SetThemeAsync(ThemeMode.Auto, TestContext.Current.CancellationToken);
        _themeAdapter.ClearReceivedCalls();

        darkModeSubject.OnNext(false);

        _themeAdapter.Received(1).Apply(ThemeVariant.Light);
    }

    [Fact]
    public async Task when_set_theme_is_called_then_current_mode_reflects_the_new_mode()
    {
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.SetThemeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);

        sut.CurrentMode.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public async Task when_set_theme_is_called_then_settings_are_persisted()
    {
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.SetThemeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);

        _ = await _settingsRepository.Received(1)
            .SaveAsync(Arg.Is<AppSettings>(s => s.ThemeMode == nameof(ThemeMode.Dark)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_initialised_with_stored_dark_theme_then_applies_dark_variant()
    {
        var stored = new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Dark) };
        _ = _settingsRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AppSettings?, ErrorResponse>.Ok(stored));
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.InitialiseAsync(TestContext.Current.CancellationToken);

        _themeAdapter.Received(1).Apply(ThemeVariant.Dark);
    }

    [Fact]
    public async Task when_initialised_with_stored_light_theme_then_applies_light_variant()
    {
        var stored = new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Light) };
        _ = _settingsRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AppSettings?, ErrorResponse>.Ok(stored));
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.InitialiseAsync(TestContext.Current.CancellationToken);

        _themeAdapter.Received(1).Apply(ThemeVariant.Light);
    }

    [Fact]
    public async Task when_initialised_with_no_stored_settings_then_falls_back_to_auto_and_consults_platform()
    {
        _ = _settingsRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AppSettings?, ErrorResponse>.Ok(null));
        _ = _platformProvider.IsDarkMode.Returns(false);
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.InitialiseAsync(TestContext.Current.CancellationToken);

        _themeAdapter.Received().Apply(ThemeVariant.Light);
    }

    [Fact]
    public async Task when_initialised_and_repository_returns_error_then_falls_back_to_auto()
    {
        _ = _settingsRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AppSettings?, ErrorResponse>.Error(new ErrorResponse("db error")));
        _ = _platformProvider.IsDarkMode.Returns(true);
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.InitialiseAsync(TestContext.Current.CancellationToken);

        _themeAdapter.Received().Apply(ThemeVariant.Dark);
    }

    [Fact]
    public async Task when_initialised_with_stored_dark_theme_then_current_mode_is_dark()
    {
        var stored = new AppSettings { Id = AppSettings.SingletonId, ThemeMode = nameof(ThemeMode.Dark) };
        _ = _settingsRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AppSettings?, ErrorResponse>.Ok(stored));
        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);

        _ = await sut.InitialiseAsync(TestContext.Current.CancellationToken);

        sut.CurrentMode.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public async Task when_switching_from_auto_to_dark_then_platform_subscription_is_cancelled()
    {
        var darkModeSubject = new Subject<bool>();
        _ = _platformProvider.IsDarkMode.Returns(false);
        _ = _platformProvider.DarkModeChanged.Returns(darkModeSubject);

        var sut = new ThemeService(_settingsRepository, _themeAdapter, _platformProvider, _logger);
        _ = await sut.SetThemeAsync(ThemeMode.Auto, TestContext.Current.CancellationToken);
        _ = await sut.SetThemeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);
        _themeAdapter.ClearReceivedCalls();

        darkModeSubject.OnNext(true);

        _themeAdapter.DidNotReceive().Apply(Arg.Any<ThemeVariant>());
    }
}
