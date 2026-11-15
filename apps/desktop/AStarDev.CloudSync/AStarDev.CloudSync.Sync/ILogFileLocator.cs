using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <summary>Says where the local log files are (#1411).</summary>
public interface ILogFileLocator
{
    /// <summary>The log folder. It may not exist yet.</summary>
    IDirectoryInfo GetLogFolder();
}
