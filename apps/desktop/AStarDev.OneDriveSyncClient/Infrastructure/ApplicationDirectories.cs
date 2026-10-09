using System.Diagnostics.CodeAnalysis;
using AStarDev.Utilities;

namespace AStarDev.OneDriveSyncClient.Infrastructure;

/// <summary>Provides the local directories used by the application.</summary>
[ExcludeFromCodeCoverage]
public static class ApplicationDirectories
{
    /// <summary>Gets the directory for log files, located in the user's application data folder.</summary>
    public static string LogsDirectory => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(ApplicationMetadata.ApplicationNameHyphenated, "logs");
}
