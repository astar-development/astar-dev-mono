namespace AStarDev.CloudSync.Logging;

/// <summary>Makes <see cref="LogEnvironment"/> values.</summary>
public static class LogEnvironmentFactory
{
    /// <summary>Makes an environment from the given values; missing folders become empty.</summary>
    /// <param name="platform">The operating system family.</param>
    /// <param name="home">The user's home folder.</param>
    /// <param name="localAppData">The user's local application data folder.</param>
    public static LogEnvironment Create(LogPlatform platform, string? home, string? localAppData) => new(platform, home ?? string.Empty, localAppData ?? string.Empty);

    /// <summary>The environment of this process.</summary>
    public static LogEnvironment CreateCurrent() => Create(LogPlatformFactory.CreateCurrent(), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
}
