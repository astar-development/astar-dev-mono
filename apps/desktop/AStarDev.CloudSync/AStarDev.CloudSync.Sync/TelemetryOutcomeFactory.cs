namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="TelemetryOutcome"/> cases.</summary>
public static class TelemetryOutcomeFactory
{
    /// <summary>Creates the succeeded outcome.</summary>
    public static TelemetryOutcome CreateSucceeded() => new TelemetryOutcome.Succeeded();

    /// <summary>Creates the failed outcome.</summary>
    public static TelemetryOutcome CreateFailed() => new TelemetryOutcome.Failed();

    /// <summary>Creates the cancelled outcome.</summary>
    public static TelemetryOutcome CreateCancelled() => new TelemetryOutcome.Cancelled();
}
