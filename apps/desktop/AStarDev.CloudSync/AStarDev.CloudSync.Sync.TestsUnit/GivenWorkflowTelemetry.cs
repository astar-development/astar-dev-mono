using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenWorkflowTelemetry : IDisposable
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly WorkflowTelemetry telemetry;
    private readonly MeterListener listener = new();
    private readonly ActivityListener activities;
    private readonly List<(string Instrument, double Value, string Tags)> measurements = [];
    private readonly ConcurrentQueue<System.Diagnostics.Activity> finished = [];

    public GivenWorkflowTelemetry()
    {
        telemetry = new WorkflowTelemetry(time);
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (ReferenceEquals(instrument.Meter.Scope, telemetry))
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => measurements.Add((instrument.Name, value, Describe(tags))));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => measurements.Add((instrument.Name, value, Describe(tags))));
        listener.Start();
        activities = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WorkflowTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = finished.Enqueue,
        };
        ActivitySource.AddActivityListener(activities);
    }

    public void Dispose()
    {
        listener.Dispose();
        activities.Dispose();
        telemetry.Dispose();
    }

    private static string Describe(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var parts = new List<string>();
        foreach (var tag in tags)
            parts.Add($"{tag.Key}={tag.Value}");

        return string.Join(',', parts.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task when_a_workflow_succeeds_then_one_run_is_counted_with_only_the_workflow_and_outcome_tags()
    {
        _ = await telemetry.TrackAsync(TelemetryWorkflowFactory.CreateOverviewRefresh(), () => Task.FromResult(1), _ => TelemetryOutcomeFactory.CreateSucceeded());

        measurements.Where(entry => entry.Instrument == "cloudsync.workflow.runs").ShouldBe([("cloudsync.workflow.runs", 1d, "outcome=succeeded,workflow=overview_refresh")]);
    }

    [Fact]
    public async Task when_a_workflow_fails_then_the_run_and_its_span_are_marked_failed()
    {
        using var testRoot = new System.Diagnostics.Activity(nameof(when_a_workflow_fails_then_the_run_and_its_span_are_marked_failed)).Start();

        _ = await telemetry.TrackAsync(TelemetryWorkflowFactory.CreateSignIn(), () => Task.FromResult(0), _ => TelemetryOutcomeFactory.CreateFailed());

        System.Diagnostics.Activity span = finished.Single(stopped => stopped.Parent == testRoot);
        (measurements.Single(entry => entry.Instrument == "cloudsync.workflow.runs").Tags, span.OperationName, span.Status).ShouldBe(("outcome=failed,workflow=sign_in", "sign_in", ActivityStatusCode.Error));
    }

    [Fact]
    public async Task when_a_workflow_takes_time_then_the_duration_is_recorded_in_milliseconds()
    {
        _ = await telemetry.TrackAsync(TelemetryWorkflowFactory.CreateFolderBrowse(), async () =>
        {
            time.Advance(TimeSpan.FromMilliseconds(250));

            return await Task.FromResult(1);
        }, _ => TelemetryOutcomeFactory.CreateSucceeded());

        measurements.Single(entry => entry.Instrument == "cloudsync.workflow.duration").Value.ShouldBe(250);
    }

    [Fact]
    public void when_a_finished_workflow_is_recorded_then_it_is_counted_without_a_span()
    {
        using var testRoot = new System.Diagnostics.Activity(nameof(when_a_finished_workflow_is_recorded_then_it_is_counted_without_a_span)).Start();

        telemetry.Record(TelemetryWorkflowFactory.CreateStartup(), TelemetryOutcomeFactory.CreateSucceeded());

        (measurements.Single(entry => entry.Instrument == "cloudsync.workflow.runs").Tags, finished.Count(stopped => stopped.Parent == testRoot)).ShouldBe(("outcome=succeeded,workflow=startup", 0));
    }
}
