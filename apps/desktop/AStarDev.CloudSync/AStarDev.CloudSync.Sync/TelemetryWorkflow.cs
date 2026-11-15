namespace AStarDev.CloudSync.Sync;

/// <summary>A user-facing workflow that CloudSync reports metrics and traces for (#1518). The cases are a fixed list, so no user data can enter a metric.</summary>
public abstract record TelemetryWorkflow
{
    private TelemetryWorkflow()
    {
    }

    /// <summary>The application starting.</summary>
    public sealed record Startup : TelemetryWorkflow
    {
        internal Startup()
        {
        }
    }

    /// <summary>Signing in to an account.</summary>
    public sealed record SignIn : TelemetryWorkflow
    {
        internal SignIn()
        {
        }
    }

    /// <summary>Browsing a folder tree.</summary>
    public sealed record FolderBrowse : TelemetryWorkflow
    {
        internal FolderBrowse()
        {
        }
    }

    /// <summary>Refreshing the overview.</summary>
    public sealed record OverviewRefresh : TelemetryWorkflow
    {
        internal OverviewRefresh()
        {
        }
    }
}
