using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenASnapshotProbe
{
    private const string ProbeName = "snapshot-probe";
    private const string FullWindowProbeName = "snapshot-probe-full-window";

    [AvaloniaFact]
    public void when_the_rendering_is_unchanged_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(CreateProbe("Snapshot probe"), ThemeVariant.Dark, ProbeName, 320, 120);
    }

    [AvaloniaFact]
    public void when_the_rendering_changes_then_the_comparison_reports_a_mismatch()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        var result = Snapshot.Compare(CreateProbe("Snapshot probe, changed"), ThemeVariant.Dark, ProbeName, 320, 120);

        var mismatch = result.ShouldBeOfType<SnapshotResult.Mismatch>();
        mismatch.DifferingPixels.ShouldBeGreaterThan(0);
    }

    [AvaloniaFact]
    public void when_the_rendering_changes_then_the_actual_and_diff_images_are_written()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        var result = Snapshot.Compare(CreateProbe("Snapshot probe, changed again"), ThemeVariant.Dark, ProbeName, 320, 120);

        var mismatch = result.ShouldBeOfType<SnapshotResult.Mismatch>();
        mismatch.Actual.Exists.ShouldBeTrue();
        mismatch.Diff.Exists.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void when_no_baseline_exists_then_the_comparison_reports_it_missing_and_writes_the_actual_image()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        var result = Snapshot.Compare(CreateProbe("Snapshot probe"), ThemeVariant.Dark, "snapshot-probe-without-baseline", 320, 120);

        var missing = result.ShouldBeOfType<SnapshotResult.MissingBaseline>();
        missing.Actual.Exists.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void when_the_rendering_differs_then_should_match_baseline_fails_naming_the_images()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        Assert.SkipWhen(Snapshot.IsUpdatingBaselines, "Baselines are being updated.");

        var failure = Should.Throw<SnapshotMismatchException>(() => Snapshot.ShouldMatchBaseline(CreateProbe("Snapshot probe, different"), ThemeVariant.Dark, ProbeName, 320, 120));

        failure.Message.ShouldContain("snapshot-probe.actual.png");
        failure.Message.ShouldContain("snapshot-probe.diff.png");
    }

    [AvaloniaFact]
    public void when_the_full_window_rendering_is_unchanged_then_it_matches_the_baseline()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        Snapshot.ShouldMatchBaseline(CreateFullWindowProbe("Backups", 200), ThemeVariant.Dark, FullWindowProbeName);
    }

    [AvaloniaFact]
    public void when_one_letter_changes_on_a_full_window_then_the_comparison_reports_a_mismatch()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        var result = Snapshot.Compare(CreateFullWindowProbe("Backup", 200), ThemeVariant.Dark, FullWindowProbeName);

        result.ShouldBeOfType<SnapshotResult.Mismatch>();
    }

    [AvaloniaFact]
    public void when_a_thin_bar_fill_changes_on_a_full_window_then_the_comparison_reports_a_mismatch()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

        var result = Snapshot.Compare(CreateFullWindowProbe("Backups", 0), ThemeVariant.Dark, FullWindowProbeName);

        result.ShouldBeOfType<SnapshotResult.Mismatch>();
    }

    [AvaloniaFact]
    public void when_an_indeterminate_progress_bar_is_captured_then_its_moving_indicator_is_stopped()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var progressBar = new ProgressBar { IsIndeterminate = true, Margin = new Avalonia.Thickness(16) };

        _ = Snapshot.Compare(new Window { Content = progressBar }, ThemeVariant.Dark, "snapshot-probe-indeterminate-progress", 320, 120);

        progressBar.IsIndeterminate.ShouldBeFalse();
    }

    private static Window CreateFullWindowProbe(string label, double barFillWidth) => new()
    {
        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = label, FontSize = 13 },
                new Border
                {
                    Width = 200,
                    Height = 4,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                    Background = Brushes.DimGray,
                    Child = new Border { Width = barFillWidth, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Background = Brushes.LightGray }
                }
            }
        }
    };

    private static Window CreateProbe(string text) => new() { Content = new TextBlock { Text = text, Margin = new Avalonia.Thickness(16), FontSize = 20 } };
}
