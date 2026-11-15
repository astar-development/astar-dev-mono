namespace AStarDev.CloudSync.Logging;

/// <summary>The operating system family that decides where the log folder is (#1411).</summary>
public abstract record LogPlatform
{
    private LogPlatform()
    {
    }

    /// <summary>Linux and other Unix systems that are not macOS.</summary>
    public sealed record Linux : LogPlatform;

    /// <summary>Windows.</summary>
    public sealed record Windows : LogPlatform;

    /// <summary>macOS.</summary>
    public sealed record MacOs : LogPlatform;
}
