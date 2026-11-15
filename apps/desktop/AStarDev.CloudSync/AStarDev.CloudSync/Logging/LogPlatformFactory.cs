namespace AStarDev.CloudSync.Logging;

/// <summary>Makes <see cref="LogPlatform"/> cases.</summary>
public static class LogPlatformFactory
{
    /// <summary>Linux and other Unix systems that are not macOS.</summary>
    public static LogPlatform CreateLinux() => new LogPlatform.Linux();

    /// <summary>Windows.</summary>
    public static LogPlatform CreateWindows() => new LogPlatform.Windows();

    /// <summary>macOS.</summary>
    public static LogPlatform CreateMacOs() => new LogPlatform.MacOs();

    /// <summary>The platform this process is running on.</summary>
    public static LogPlatform CreateCurrent()
    {
        if (OperatingSystem.IsWindows())
            return CreateWindows();

        return OperatingSystem.IsMacOS() ? CreateMacOs() : CreateLinux();
    }
}
