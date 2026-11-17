using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAReclassifyProgressSource
{
    private readonly ReclassifyProgressSource source = new();
    private readonly ReclassifyRecordingProgress downstream = new();

    private static ReclassifyProgress Report(int filesDone) => ReclassifyProgressFactory.Create(filesDone, 1);

    [Fact]
    public void when_nothing_has_run_then_there_is_no_progress()
    {
        source.Current.ShouldBe(Option.None<ReclassifyProgress>());
    }

    [Fact]
    public void when_a_run_reports_then_the_latest_report_is_kept_downstream_gets_it_and_a_change_is_raised()
    {
        var changes = 0;
        source.Changed += (_, _) => changes++;
        using var scope = source.Begin(downstream);

        scope.Report(Report(10));
        scope.Report(Report(20));

        (source.Current, downstream.Reports.Count, changes).ShouldBe((Option.Some(Report(20)), 2, 2));
    }

    [Fact]
    public void when_a_run_that_reported_ends_then_the_progress_clears_and_a_change_is_raised()
    {
        var changes = 0;
        var scope = source.Begin(downstream);
        scope.Report(Report(10));
        source.Changed += (_, _) => changes++;

        scope.Dispose();

        (source.Current, changes).ShouldBe((Option.None<ReclassifyProgress>(), 1));
    }

    [Fact]
    public void when_a_run_that_never_reported_ends_then_the_progress_of_the_active_run_is_kept()
    {
        using var active = source.Begin(downstream);
        active.Report(Report(10));
        var refused = source.Begin(downstream);

        refused.Dispose();

        source.Current.ShouldBe(Option.Some(Report(10)));
    }

    [Fact]
    public void when_a_run_reports_after_it_ended_then_the_report_is_ignored_by_the_source_but_still_forwarded()
    {
        var scope = source.Begin(downstream);
        scope.Report(Report(10));
        scope.Dispose();

        scope.Report(Report(20));

        (source.Current, downstream.Reports.Count).ShouldBe((Option.None<ReclassifyProgress>(), 2));
    }

    [Fact]
    public void when_a_second_run_takes_over_then_the_first_run_ending_does_not_clear_it()
    {
        var first = source.Begin(downstream);
        first.Report(Report(10));
        using var second = source.Begin(downstream);
        second.Report(Report(30));

        first.Dispose();

        source.Current.ShouldBe(Option.Some(Report(30)));
    }

    [Fact]
    public async Task when_many_threads_report_at_once_then_every_report_is_forwarded_and_the_snapshot_is_one_of_them()
    {
        var changes = 0;
        source.Changed += (_, _) => Interlocked.Increment(ref changes);
        var counted = new CountingProgress();
        using var counting = source.Begin(counted);
        var reports = Enumerable.Range(1, 50).Select(index => Task.Run(() => counting.Report(Report(index)), TestContext.Current.CancellationToken)).ToArray();

        await Task.WhenAll(reports);

        var last = source.Current.Match(report => report.FilesDone, () => 0);
        (counted.Count, changes, last is >= 1 and <= 50).ShouldBe((50, 50, true));
    }

    private sealed class CountingProgress : IProgress<ReclassifyProgress>
    {
        private int count;

        public int Count => Volatile.Read(ref count);

        public void Report(ReclassifyProgress value) => Interlocked.Increment(ref count);
    }
}
