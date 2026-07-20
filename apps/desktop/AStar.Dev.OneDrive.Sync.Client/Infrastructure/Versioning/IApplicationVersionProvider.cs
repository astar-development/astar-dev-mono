namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Versioning;

/// <summary>
/// Provides the currently running application version, sourced from the entry assembly.
/// </summary>
public interface IApplicationVersionProvider
{
    /// <summary>
    /// The currently running application version.
    /// </summary>
    string CurrentVersion { get; }
}
