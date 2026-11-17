using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenARunRecordingReclassifier
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 30, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private readonly FakeClassificationReclassifier inner = new();
    private readonly SeedingStateStore seedState = new() { Version = "v1" };
    private readonly ReclassifyProgressSource progressSource = new();
    private readonly RecordingLogger<RunRecordingReclassifier> logger = new();

    private RunRecordingReclassifier Create(IReclassifyRunStore runs) => Create(runs, seedState);

    private RunRecordingReclassifier Create(IReclassifyRunStore runs, ISeedStateStore seeds) => new(inner, seeds, runs, ReclassifyOptionsFactory.Create(10, 1, TimeSpan.Zero, TimeSpan.Zero, new FakeTimeProvider(Now)), progressSource, logger);

    private static ReclassifyCounts Counts() => ReclassifyCountsFactory.Create(3, 1, 0);

    public static TheoryData<string> UnfinishedOutcomes => ["cancelled", "failed", "already running"];

    private static ReclassifyOutcome Unfinished(string outcome) => outcome switch
    {
        "cancelled" => ReclassifyOutcomeFactory.CreateCancelled(Counts()),
        "failed" => ReclassifyOutcomeFactory.CreateFailed(Counts()),
        _ => ReclassifyOutcomeFactory.CreateAlreadyRunning()
    };

    [Fact]
    public async Task when_waiting_until_idle_then_the_inner_reclassifier_is_waited_on()
    {
        await Create(new InMemoryReclassifyRunStore()).WaitUntilIdleAsync(CancellationToken);

        inner.WaitCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_run_completes_then_the_engine_version_the_seed_version_and_the_finish_time_are_recorded()
    {
        var runs = new InMemoryReclassifyRunStore();

        _ = await Create(runs).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        runs.Last.ShouldBe(Option.Some(ReclassifyRunRecordFactory.Create(ClassificationEngineVersion.Current, Option.Some("v1"), Now)));
    }

    [Fact]
    public async Task when_a_run_completes_then_the_inner_outcome_is_returned()
    {
        inner.Outcome = ReclassifyOutcomeFactory.CreateCompleted(Counts());

        var outcome = await Create(new InMemoryReclassifyRunStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        outcome.ShouldBeSameAs(inner.Outcome);
    }

    [Theory]
    [MemberData(nameof(UnfinishedOutcomes))]
    public async Task when_a_run_does_not_complete_then_nothing_is_recorded(string unfinished)
    {
        inner.Outcome = Unfinished(unfinished);
        var runs = new InMemoryReclassifyRunStore();

        var outcome = await Create(runs).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        (outcome, runs.SetCalls).ShouldBe((inner.Outcome, 0));
    }

    [Fact]
    public async Task when_the_seed_changes_during_the_run_then_the_seed_version_from_the_start_is_recorded()
    {
        inner.Gate = new TaskCompletionSource();
        var runs = new InMemoryReclassifyRunStore();

        var running = Create(runs).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);
        await inner.Started.Task;
        seedState.Version = "v2";
        inner.Gate.SetResult();
        _ = await running;

        runs.Last.Match(run => run.SeedVersion, () => Option.None<string>()).ShouldBe(Option.Some("v1"));
    }

    [Fact]
    public async Task when_no_seed_is_applied_then_the_run_is_recorded_without_a_seed_version()
    {
        var runs = new InMemoryReclassifyRunStore();

        _ = await Create(runs, new SeedingStateStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        runs.Last.Match(run => run.SeedVersion, () => Option.Some("not recorded")).ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_the_seed_version_cannot_be_read_then_the_run_is_recorded_without_a_seed_version()
    {
        var runs = new InMemoryReclassifyRunStore();

        _ = await Create(runs, new SeedingStateStore { FailReads = true }).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        runs.Last.Match(run => run.SeedVersion, () => Option.Some("not recorded")).ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_the_seed_read_is_cancelled_then_the_run_still_starts_and_nothing_throws()
    {
        inner.Outcome = ReclassifyOutcomeFactory.CreateCancelled(Counts());

        var outcome = await Create(new InMemoryReclassifyRunStore(), new CancellingSeedStateStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        (outcome, inner.StartCount).ShouldBe((inner.Outcome, 1));
    }

    [Fact]
    public async Task when_the_record_cannot_be_written_then_the_run_still_completes_and_a_warning_is_logged()
    {
        inner.Outcome = ReclassifyOutcomeFactory.CreateCompleted(Counts());

        var outcome = await Create(new InMemoryReclassifyRunStore { FailWrites = true }).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        outcome.ShouldBeSameAs(inner.Outcome);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task when_the_record_write_is_cancelled_then_the_completed_outcome_is_still_returned()
    {
        inner.Outcome = ReclassifyOutcomeFactory.CreateCompleted(Counts());

        var outcome = await Create(new CancellingReclassifyRunStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        outcome.ShouldBeSameAs(inner.Outcome);
    }

    [Fact]
    public async Task when_the_caller_cancels_after_the_run_completed_then_the_run_is_still_recorded()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var runs = new TokenHonouringReclassifyRunStore();

        _ = await Create(runs).StartAsync(new Progress<ReclassifyProgress>(), cancelled.Token);

        runs.Last.Match(run => run.EngineVersion, () => "not recorded").ShouldBe(ClassificationEngineVersion.Current);
    }

    [Fact]
    public async Task when_a_run_is_recorded_then_the_run_recorded_event_is_raised_after_the_record_is_written()
    {
        var runs = new InMemoryReclassifyRunStore();
        var recordedAtRaise = Option.None<ReclassifyRunRecord>();
        var reclassifier = Create(runs);
        reclassifier.RunRecorded += (_, _) => recordedAtRaise = runs.Last;

        _ = await reclassifier.StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        recordedAtRaise.ShouldBe(runs.Last);
        recordedAtRaise.Match(_ => true, () => false).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(UnfinishedOutcomes))]
    public async Task when_a_run_does_not_complete_then_the_run_recorded_event_is_not_raised(string unfinished)
    {
        inner.Outcome = Unfinished(unfinished);
        var raised = 0;
        var reclassifier = Create(new InMemoryReclassifyRunStore());
        reclassifier.RunRecorded += (_, _) => raised++;

        _ = await reclassifier.StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        raised.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_record_cannot_be_written_then_the_run_recorded_event_is_not_raised()
    {
        var raised = 0;
        var reclassifier = Create(new InMemoryReclassifyRunStore { FailWrites = true });
        reclassifier.RunRecorded += (_, _) => raised++;

        _ = await reclassifier.StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        raised.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_run_reports_then_the_caller_and_the_progress_source_both_see_it_and_the_snapshot_clears_at_the_end()
    {
        inner.Reports = [ReclassifyProgressFactory.Create(500, 1)];
        inner.Gate = new TaskCompletionSource();
        var caller = new ReclassifyRecordingProgress();

        var running = Create(new InMemoryReclassifyRunStore()).StartAsync(caller, CancellationToken);
        await inner.Started.Task;
        var during = progressSource.Current;
        inner.Gate.SetResult();
        await running;

        (during, caller.Reports.Single(), progressSource.Current).ShouldBe((Option.Some(ReclassifyProgressFactory.Create(500, 1)), (500, 1, 0), Option.None<ReclassifyProgress>()));
    }

    [Theory]
    [MemberData(nameof(UnfinishedOutcomes))]
    public async Task when_a_run_does_not_complete_then_the_progress_snapshot_is_cleared(string unfinished)
    {
        inner.Reports = [ReclassifyProgressFactory.Create(500, 1)];
        inner.Outcome = Unfinished(unfinished);

        _ = await Create(new InMemoryReclassifyRunStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        progressSource.Current.ShouldBe(Option.None<ReclassifyProgress>());
    }

    [Fact]
    public async Task when_a_second_run_is_refused_while_the_first_reports_then_the_first_snapshot_stays()
    {
        inner.Reports = [ReclassifyProgressFactory.Create(500, 1)];
        var firstGate = new TaskCompletionSource();
        inner.Gate = firstGate;
        var reclassifier = Create(new InMemoryReclassifyRunStore());
        var first = reclassifier.StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);
        await inner.Started.Task;
        inner.Reports = [];
        inner.Gate = null;
        inner.Outcome = ReclassifyOutcomeFactory.CreateAlreadyRunning();

        _ = await reclassifier.StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        var during = progressSource.Current;
        firstGate.SetResult();
        await first;

        during.ShouldBe(Option.Some(ReclassifyProgressFactory.Create(500, 1)));
    }
}
