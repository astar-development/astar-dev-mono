using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenASnapshotProbe
{
    private const string ProbeName = "snapshot-probe";

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

    private static Window CreateProbe(string text) => new() { Content = new TextBlock { Text = text, Margin = new Avalonia.Thickness(16), FontSize = 20 } };
}
