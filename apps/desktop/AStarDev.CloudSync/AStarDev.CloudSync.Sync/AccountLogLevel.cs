namespace AStarDev.CloudSync.Sync;

/// <summary>How much an account writes to the local log files (#1411).</summary>
public abstract record AccountLogLevel
{
    private AccountLogLevel()
    {
    }

    /// <summary>Warnings and errors only.</summary>
    public sealed record Standard : AccountLogLevel
    {
        internal Standard()
        {
        }
    }

    /// <summary>Everything, down to debug detail. It stays in the local files and never goes to Application Insights.</summary>
    public sealed record Verbose : AccountLogLevel
    {
        internal Verbose()
        {
        }
    }
}
