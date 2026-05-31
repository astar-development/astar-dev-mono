using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Theme;

public sealed class GivenAThemeService
{
    [Fact]
    public void when_constructed_then_current_theme_is_system()
    {
        var service = new ThemeService();

        service.CurrentTheme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public void when_apply_called_with_light_theme_then_current_theme_is_light()
    {
        var service = new ThemeService();

        service.Apply(AppTheme.Light);

        service.CurrentTheme.ShouldBe(AppTheme.Light);
    }

    [Fact]
    public void when_apply_called_with_dark_theme_then_current_theme_is_dark()
    {
        var service = new ThemeService();

        service.Apply(AppTheme.Dark);

        service.CurrentTheme.ShouldBe(AppTheme.Dark);
    }

    [Fact]
    public void when_apply_called_with_system_theme_then_current_theme_is_system()
    {
        var service = new ThemeService();

        service.Apply(AppTheme.System);

        service.CurrentTheme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public void when_apply_called_then_theme_changed_event_is_raised()
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

    [Fact]
    public void when_apply_called_with_hacker_theme_then_current_theme_is_hacker()
    {
        var service = new ThemeService();

        service.Apply(AppTheme.Hacker);

        service.CurrentTheme.ShouldBe(AppTheme.Hacker);
    }

    [Theory]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.System)]
    [InlineData(AppTheme.Hacker)]
    public void when_apply_called_with_any_theme_then_event_is_raised(AppTheme theme)
    {
        var service = new ThemeService();
        bool eventRaised = false;

        service.ThemeChanged += (s, t) => eventRaised = true;

        service.Apply(theme);

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void when_apply_called_multiple_times_with_same_theme_then_event_is_raised_each_time()
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
    public void when_apply_called_with_alternating_themes_then_current_theme_updates_correctly()
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
    public void when_service_is_checked_then_it_is_disposable()
    {
        var service = new ThemeService();

        _ = service.ShouldBeAssignableTo<IDisposable>();
    }

    [Fact]
    public void when_dispose_called_then_no_exception_is_thrown()
    {
        var service = new ThemeService();

        service.Dispose(); // Should not throw
    }

    [Fact]
    public void when_apply_called_after_dispose_then_object_disposed_exception_may_be_thrown()
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
