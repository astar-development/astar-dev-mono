using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Localisation;

public sealed class GivenALocalisationService
{
    private readonly IAppSettingsRepository _settingsRepository = Substitute.For<IAppSettingsRepository>();
    private readonly LocalisationService    _sut;

    public GivenALocalisationService()
    {
        _ = _settingsRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AppSettings?, ErrorResponse>.Ok(null));

        _ = _settingsRepository.SaveAsync(Arg.Any<AppSettings>(), Arg.Any<CancellationToken>())
            .Returns(call => new Result<AppSettings, ErrorResponse>.Ok(call.Arg<AppSettings>()));

        _sut = new LocalisationService(_settingsRepository, NullLogger<LocalisationService>.Instance);
    }

    [Fact]
    public async Task when_an_unsupported_locale_is_requested_then_returns_error()
    {
        var result = await _sut.SetLocaleAsync("fr-FR", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<string, ErrorResponse>.Error>();
    }

    [Fact]
    public async Task when_a_supported_locale_is_requested_then_returns_ok()
    {
        var result = await _sut.SetLocaleAsync("en-GB", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<string, ErrorResponse>.Ok>();
    }

    [Fact]
    public async Task when_a_supported_locale_is_set_then_current_locale_reflects_new_locale()
    {
        _ = await _sut.SetLocaleAsync("en-GB", TestContext.Current.CancellationToken);

        _sut.CurrentLocale.ShouldBe("en-GB");
    }

    [Fact]
    public async Task when_locale_is_set_then_locale_changed_event_is_fired()
    {
        bool eventFired = false;
        _sut.LocaleChanged += (_, _) => eventFired = true;

        _ = await _sut.SetLocaleAsync("en-GB", TestContext.Current.CancellationToken);

        eventFired.ShouldBeTrue();
    }

    [Fact]
    public async Task when_an_unsupported_locale_is_requested_then_settings_are_not_persisted()
    {
        _ = await _sut.SetLocaleAsync("fr-FR", TestContext.Current.CancellationToken);

        _ = await _settingsRepository.DidNotReceive().SaveAsync(Arg.Any<AppSettings>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_default_locale_is_en_gb()
    {
        _sut.CurrentLocale.ShouldBe("en-GB");
    }

    [Fact]
    public void when_constructed_then_supported_locales_contains_en_gb()
    {
        _sut.SupportedLocales.ShouldContain("en-GB");
    }
}
