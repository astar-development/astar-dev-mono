namespace AStarDev.CloudSync.Sync;

/// <summary>Counts and times workflows with bounded tags only: the workflow and its outcome. It never takes a name, path, URL or id (#1518).</summary>
public interface IWorkflowTelemetry
{
    /// <summary>Runs the operation inside a trace span and records its count and duration.</summary>
    /// <typeparam name="TResult">What the operation returns.</typeparam>
    /// <param name="workflow">The workflow being run.</param>
    /// <param name="operation">The operation.</param>
    /// <param name="outcomeOf">Says how the result ended.</param>
    Task<TResult> TrackAsync<TResult>(TelemetryWorkflow workflow, Func<Task<TResult>> operation, Func<TResult, TelemetryOutcome> outcomeOf);

    /// <summary>Counts a workflow that has already finished.</summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="outcome">How it ended.</param>
    void Record(TelemetryWorkflow workflow, TelemetryOutcome outcome);
}
