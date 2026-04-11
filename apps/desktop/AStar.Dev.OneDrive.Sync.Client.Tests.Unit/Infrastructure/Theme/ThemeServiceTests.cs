using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services;

public sealed class ThemeServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithSystemTheme()
    {
        var service = new ThemeService();

        service.CurrentTheme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public void Apply_WithLightTheme_ShouldChangeCurrentTheme()
    {
        var service = new ThemeService();

        service.Apply(AppTheme.Light);

        service.CurrentTheme.ShouldBe(AppTheme.Light);
    }

    [Fact]
    public void Apply_WithDarkTheme_ShouldChangeCurrentTheme()
    {
        var service = new ThemeService();

        service.Apply(AppTheme.Dark);

        service.CurrentTheme.ShouldBe(AppTheme.Dark);
    }

    [Fact]
    public void Apply_WithSystemTheme_ShouldChangeCurrentTheme()
    {
        var service = new ThemeService();

        service.Apply(AppTheme.System);

        service.CurrentTheme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public void Apply_ShouldRaiseThemeChangedEvent()
    {
        var service = new ThemeService();
        bool eventRaised = false;
        AppTheme? raisedTheme = null;

        service.ThemeChanged += (s, theme) =>
        {
            eventRaised = true;
            raisedTheme = theme;
        };

        service.Apply(AppTheme.Dark);

        eventRaised.ShouldBeTrue();
        raisedTheme.ShouldBe(AppTheme.Dark);
    }

    [Theory]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.System)]
    public void Apply_WithAnyTheme_ShouldRaiseEvent(AppTheme theme)
    {
        var service = new ThemeService();
        bool eventRaised = false;

        service.ThemeChanged += (s, t) => eventRaised = true;

        service.Apply(theme);

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Apply_MultipleTimesWithSameTheme_ShouldRaiseEventEachTime()
    {
        var service = new ThemeService();
        int eventCount = 0;

        service.ThemeChanged += (s, t) => eventCount++;

        service.Apply(AppTheme.Dark);
        service.Apply(AppTheme.Dark);
        service.Apply(AppTheme.Dark);

        eventCount.ShouldBe(3);
    }

    [Fact]
    public void Apply_AlternatingThemes_ShouldUpdateCurrentThemeCorrectly()
    {
        var service = new ThemeService();

        service.Apply(AppTheme.Light);
        service.CurrentTheme.ShouldBe(AppTheme.Light);

        service.Apply(AppTheme.Dark);
        service.CurrentTheme.ShouldBe(AppTheme.Dark);

        service.Apply(AppTheme.Light);

        service.CurrentTheme.ShouldBe(AppTheme.Light);
    }

    [Fact]
    public void ThemeService_ShouldBeDisposable()
    {
        var service = new ThemeService();

        _ = service.ShouldBeAssignableTo<IDisposable>();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var service = new ThemeService();

        service.Dispose(); // Should not throw
    }

    [Fact]
    public void Apply_AfterDispose_BehaviorIsUndefined()
    {
        var service = new ThemeService();
        service.Dispose();
        try
        {
            service.Apply(AppTheme.Light);
        }
        catch(ObjectDisposedException)
        {
            // Expected
        }
    }
}
