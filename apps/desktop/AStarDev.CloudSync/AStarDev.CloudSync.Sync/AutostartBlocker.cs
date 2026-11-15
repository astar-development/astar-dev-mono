namespace AStarDev.CloudSync.Sync;

/// <summary>Why the app cannot start itself at login (#1410).</summary>
public abstract record AutostartBlocker
{
    private AutostartBlocker()
    {
    }

    /// <summary>This is a development or portable build. Only an installed build registers itself, so a build run from a source folder never leaves an entry behind.</summary>
    public sealed record NotInstalledBuild : AutostartBlocker
    {
        internal NotInstalledBuild()
        {
        }
    }

    /// <summary>There is no registration for this operating system yet.</summary>
    public sealed record PlatformNotSupported : AutostartBlocker
    {
        internal PlatformNotSupported()
        {
        }
    }
}
