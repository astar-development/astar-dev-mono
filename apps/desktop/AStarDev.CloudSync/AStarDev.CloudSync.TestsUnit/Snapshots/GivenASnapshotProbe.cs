using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

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
    public void when_an_endless_animation_runs_before_the_capture_then_it_stays_on_its_first_frame()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var fadingBorder = new Border { Width = 40, Height = 40, Background = Brushes.LightGray };
        fadingBorder.Styles.Add(CreateEndlessFade());
        var window = new Window { Content = fadingBorder };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(300);

        _ = Snapshot.Compare(window, ThemeVariant.Dark, "snapshot-probe-endless-animation", 120, 120);

        fadingBorder.Opacity.ShouldBe(0);
    }

    private static Style CreateEndlessFade() => new(selector => selector.OfType<Border>())
    {
        Animations =
        {
            new Animation
            {
                Duration = TimeSpan.FromSeconds(1),
                IterationCount = IterationCount.Infinite,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Avalonia.Visual.OpacityProperty, 0d) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Avalonia.Visual.OpacityProperty, 1d) } }
                }
            }
        }
    };

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
