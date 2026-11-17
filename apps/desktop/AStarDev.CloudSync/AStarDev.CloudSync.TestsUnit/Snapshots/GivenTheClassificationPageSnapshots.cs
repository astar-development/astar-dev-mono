using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheClassificationPageSnapshots
{
    private static Window Host(ClassificationPageViewModel page) => new()
    {
        Content = new Border
        {
            Padding = new Avalonia.Thickness(16),
            [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncSurfaceBrush"),
            Child = new ClassificationPageContent { DataContext = page }
        }
    };

    private static ThemeVariant Theme(string theme) => theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static ReclassifyCounts Counts(int files, int batches) => ReclassifyCountsFactory.Create(files, batches, 0);

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_idle_then_the_page_header_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var page = TestData.CreateClassificationPage();
        await page.LoadCommand.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(Host(page), Theme(theme), $"classification-page-idle-{theme}", 900, 300);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_running_then_the_page_header_shows_progress_and_cancel_and_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var reclassifier = new FakeClassificationReclassifier { Reports = [ReclassifyProgressFactory.Create(1500, 3)], Gate = new TaskCompletionSource() };
        var page = TestData.CreateClassificationPage(reclassifier);
        await page.LoadCommand.ExecuteAsync(null);
        var running = page.Reclassify.ReclassifyCommand.ExecuteAsync(null);
        await reclassifier.Started.Task;
        Dispatcher.UIThread.RunJobs();

        Snapshot.ShouldMatchBaseline(Host(page), Theme(theme), $"classification-page-running-{theme}", 900, 300);
        page.Reclassify.CancelCommand.Execute(null);
        await running;
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_completed_then_the_page_header_shows_the_counts_and_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var page = TestData.CreateClassificationPage(new FakeClassificationReclassifier { Outcome = ReclassifyOutcomeFactory.CreateCompleted(Counts(1234, 3)) });
        await page.LoadCommand.ExecuteAsync(null);
        await page.Reclassify.ReclassifyCommand.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(Host(page), Theme(theme), $"classification-page-completed-{theme}", 900, 300);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_completed_with_skipped_files_then_the_page_header_shows_the_skipped_count_and_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var page = TestData.CreateClassificationPage(new FakeClassificationReclassifier { Outcome = ReclassifyOutcomeFactory.CreateCompleted(ReclassifyCountsFactory.Create(1234, 3, 0, 2500)) });
        await page.LoadCommand.ExecuteAsync(null);
        await page.Reclassify.ReclassifyCommand.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(Host(page), Theme(theme), $"classification-page-completed-skipped-{theme}", 900, 300);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_failed_then_the_page_header_shows_the_failure_and_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var page = TestData.CreateClassificationPage(new FakeClassificationReclassifier { Outcome = ReclassifyOutcomeFactory.CreateFailed(Counts(40, 1)) });
        await page.LoadCommand.ExecuteAsync(null);
        await page.Reclassify.ReclassifyCommand.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(Host(page), Theme(theme), $"classification-page-failed-{theme}", 900, 300);
    }
}
