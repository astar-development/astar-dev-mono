namespace AStarDev.CloudSync.Sync;

/// <summary>How a workflow ended, for a metric tag.</summary>
public abstract record TelemetryOutcome
{
    private TelemetryOutcome()
    {
    }

    /// <summary>The workflow did what it was asked to do.</summary>
    public sealed record Succeeded : TelemetryOutcome
    {
        internal Succeeded()
        {
        }
    }

    /// <summary>The workflow ended with an error.</summary>
    public sealed record Failed : TelemetryOutcome
    {
        internal Failed()
        {
        }
    }

    /// <summary>The user cancelled the workflow; it is not an error.</summary>
    public sealed record Cancelled : TelemetryOutcome
    {
        internal Cancelled()
        {
        }
    }
}
