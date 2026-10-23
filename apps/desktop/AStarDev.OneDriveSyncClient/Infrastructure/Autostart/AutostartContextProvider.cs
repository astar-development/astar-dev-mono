using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public sealed class AutostartContextProvider : IAutostartContextProvider
{
    /// <inheritdoc />
    public AutostartContext Current() => AutostartContextFactory.Create(DesktopPlatformFactory.CreateCurrent(), Environment.ProcessPath, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"));
}
