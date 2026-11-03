using AStarDev.OneDriveSyncClient.Clock;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Clock;

public sealed class GivenAClockViewModel
{
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly AppSettings _settings = new();

    public GivenAClockViewModel() => _settingsService.Current.Returns(_settings);

    private ClockViewModel CreateSut() => new(_settingsService);

    [Fact]
    public void when_settings_are_default_then_an_analogue_clock_with_seconds_is_shown()
    {
        var sut = CreateSut();

        sut.IsAnalogue.ShouldBeTrue();
        sut.IsDigital.ShouldBeFalse();
        sut.ShowSeconds.ShouldBeTrue();
    }

    [Fact]
    public void when_settings_specify_digital_without_seconds_then_the_view_model_reflects_them()
    {
        _settings.ClockStyle = "Digital";
        _settings.ShowClockSeconds = false;

        var sut = CreateSut();

        sut.IsDigital.ShouldBeTrue();
        sut.IsAnalogue.ShouldBeFalse();
        sut.ShowSeconds.ShouldBeFalse();
    }

    [Fact]
    public void when_settings_change_then_the_style_and_seconds_update()
    {
        var sut = CreateSut();
        _settings.ClockStyle = "Digital";
        _settings.ShowClockSeconds = false;

        _settingsService.SettingsChanged += Raise.Event<EventHandler<AppSettings>>(_settingsService, _settings);

        sut.IsDigital.ShouldBeTrue();
        sut.ShowSeconds.ShouldBeFalse();
    }

    [Fact]
    public void when_settings_change_then_property_changed_is_raised_for_the_style_and_seconds()
    {
        var sut = CreateSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, args) => raised.Add(args.PropertyName);
        _settings.ClockStyle = "Digital";

        _settingsService.SettingsChanged += Raise.Event<EventHandler<AppSettings>>(_settingsService, _settings);

        raised.ShouldContain(nameof(ClockViewModel.IsDigital));
        raised.ShouldContain(nameof(ClockViewModel.IsAnalogue));
        raised.ShouldContain(nameof(ClockViewModel.ShowSeconds));
    }
}
