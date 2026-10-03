namespace AStarDev.Utilities;

/// <summary>Provides methods to retrieve application-specific paths for data storage, logs, and user files based on the operating system.</summary>
public static class ApplicationPathsProvider
{
    /// <param name="applicationName"></param>
#pragma warning disable CA1034
    extension(string applicationName)
#pragma warning restore CA1034
    {
        /// <summary>Gets the application-specific data directory.</summary>
        /// <returns>The path to the application-specific data directory.</returns>
        public string ApplicationDirectory() => GetPlatformDataDirectory(applicationName);

        /// <summary>Gets the application-specific logs directory.</summary>
        /// <returns>The path to the application-specific logs directory.</returns>
        public string LogsDirectory() => ResolveLogsDirectory(applicationName);

        /// <summary>Gets the application-specific user directory.</summary>
        /// <returns>The path to the application-specific user directory.</returns>
        public string UserDirectory() => ResolveUsersDirectory(applicationName);
    }

    private static string ResolveLogsDirectory(string applicationName)
    {
        string logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(applicationName, "logs");

        _ = Directory.CreateDirectory(logDirectory);

        return logDirectory;
    }

    private static string ResolveUsersDirectory(string applicationName)
    {
        string usersDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).CombinePath(applicationName, "sync");

        _ = Directory.CreateDirectory(usersDirectory);

        return usersDirectory;
    }

    private static string GetPlatformDataDirectory(string applicationName)
    {
        string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string directory = OperatingSystem.IsWindows()
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                applicationName)
            : SetNonWindowsPath(applicationName, Home);
        _ = Directory.CreateDirectory(directory);

        return directory;
    }

    private static string SetNonWindowsPath(string applicationName, string Home) => OperatingSystem.IsMacOS()
                    ? Path.Combine(Home, "Library", "Application Support", applicationName)
                    : Path.Combine(Home, ".config", applicationName);
}
