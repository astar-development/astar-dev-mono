using Avalonia;
using Avalonia.Headless.XUnit;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenTheTestApp
{
    [AvaloniaFact]
    public void when_the_test_app_starts_then_the_cloud_sync_design_tokens_are_loaded()
    {
        var application = Application.Current.ShouldNotBeNull();

        var found = application.TryGetResource("CloudSyncAccentBrush", application.ActualThemeVariant, out var accentBrush);

        found.ShouldBeTrue();
        accentBrush.ShouldNotBeNull();
    }
}
