namespace AStarDev.CloudSync.Logging;

/// <summary>What decides where the log folder is: the platform and the user's folders (#1411).</summary>
/// <param name="Platform">The operating system family.</param>
/// <param name="Home">The user's home folder.</param>
/// <param name="LocalAppData">The user's local application data folder (<c>%LOCALAPPDATA%</c> on Windows).</param>
public sealed record LogEnvironment(LogPlatform Platform, string Home, string LocalAppData);
