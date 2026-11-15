namespace AStarDev.CloudSync.Sync;

internal static class TelemetryTags
{
    internal static string TagValue(this TelemetryWorkflow workflow) => workflow switch
    {
        TelemetryWorkflow.Startup => "startup",
        TelemetryWorkflow.SignIn => "sign_in",
        TelemetryWorkflow.FolderBrowse => "folder_browse",
        _ => "overview_refresh",
    };

    internal static string TagValue(this TelemetryOutcome outcome) => outcome switch
    {
        TelemetryOutcome.Succeeded => "succeeded",
        TelemetryOutcome.Cancelled => "cancelled",
        _ => "failed",
    };
}
