using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class WorkflowTelemetry : IWorkflowTelemetry, IDisposable
{
    /// <summary>The name of the meter and of the activity source.</summary>
    public const string SourceName = "AStarDev.CloudSync";

    private readonly TimeProvider timeProvider;
    private readonly Meter meter;
    private readonly ActivitySource source = new(SourceName);
    private readonly Counter<long> runs;
    private readonly Histogram<double> duration;

    /// <summary>Creates the meter, the activity source and the instruments.</summary>
    /// <param name="timeProvider">Times the workflows.</param>
    public WorkflowTelemetry(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
        meter = new Meter(SourceName, null, null, this);
        runs = meter.CreateCounter<long>("cloudsync.workflow.runs", description: "Workflow runs by workflow and outcome.");
        duration = meter.CreateHistogram<double>("cloudsync.workflow.duration", unit: "ms", description: "Workflow duration by workflow and outcome.");
    }

    /// <inheritdoc />
    public async Task<TResult> TrackAsync<TResult>(TelemetryWorkflow workflow, Func<Task<TResult>> operation, Func<TResult, TelemetryOutcome> outcomeOf)
    {
        var started = timeProvider.GetTimestamp();
        using var activity = source.StartActivity(workflow.TagValue());
        var result = await operation().ConfigureAwait(false);
        var outcome = outcomeOf(result);
        _ = activity?.SetTag("outcome", outcome.TagValue());
        _ = activity?.SetStatus(outcome is TelemetryOutcome.Failed ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
        Count(workflow, outcome, timeProvider.GetElapsedTime(started));

        return result;
    }

    /// <inheritdoc />
    public void Record(TelemetryWorkflow workflow, TelemetryOutcome outcome) => Count(workflow, outcome, TimeSpan.Zero);

    /// <summary>Stops the meter and the activity source.</summary>
    public void Dispose()
    {
        meter.Dispose();
        source.Dispose();
    }

    private void Count(TelemetryWorkflow workflow, TelemetryOutcome outcome, TimeSpan elapsed)
    {
        var tags = new TagList { { "workflow", workflow.TagValue() }, { "outcome", outcome.TagValue() } };
        runs.Add(1, tags);
        duration.Record(elapsed.TotalMilliseconds, tags);
    }
}
