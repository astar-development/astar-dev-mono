namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="TelemetryWorkflow"/> cases.</summary>
public static class TelemetryWorkflowFactory
{
    /// <summary>Creates the startup workflow.</summary>
    public static TelemetryWorkflow CreateStartup() => new TelemetryWorkflow.Startup();

    /// <summary>Creates the sign-in workflow.</summary>
    public static TelemetryWorkflow CreateSignIn() => new TelemetryWorkflow.SignIn();

    /// <summary>Creates the folder browsing workflow.</summary>
    public static TelemetryWorkflow CreateFolderBrowse() => new TelemetryWorkflow.FolderBrowse();

    /// <summary>Creates the overview refresh workflow.</summary>
    public static TelemetryWorkflow CreateOverviewRefresh() => new TelemetryWorkflow.OverviewRefresh();
}
