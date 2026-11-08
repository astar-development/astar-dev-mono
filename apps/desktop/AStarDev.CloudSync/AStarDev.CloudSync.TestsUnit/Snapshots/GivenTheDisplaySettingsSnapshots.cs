using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheDisplaySettingsSnapshots
{
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void when_rendered_then_the_display_settings_match_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncSurfaceBrush"),
                Child = new DisplaySettingsView { DataContext = TestData.CreateDisplaySettings() }
            }
        };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"control-display-settings-{theme}", 280, 280);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void when_a_plaintext_consent_is_remembered_then_the_display_settings_with_the_withdraw_setting_match_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncSurfaceBrush"),
                Child = new DisplaySettingsView { DataContext = TestData.CreateDisplaySettings(new FakePlaintextCacheConsentSettings { Consent = PlaintextCacheConsentFactory.CreateGranted() }) }
            }
        };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"control-display-settings-plaintext-consent-{theme}", 280, 440);
    }
}
