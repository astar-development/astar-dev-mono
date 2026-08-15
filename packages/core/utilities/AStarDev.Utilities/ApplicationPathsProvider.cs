namespace AStarDev.Utilities;

/// <summary>
///
/// </summary>
public static class ApplicationPathsProvider
{
    /// <param name="applicationName"></param>
#pragma warning disable CA1034
    extension(string applicationName)
#pragma warning restore CA1034
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string ApplicationDirectory() => GetPlatformDataDirectory(applicationName);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string LogsDirectory() => ResolveLogsDirectory(applicationName);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
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
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string directory = OperatingSystem.IsWindows()
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                applicationName)
            : SetNonWindowsPath(applicationName, home);
        _ = Directory.CreateDirectory(directory);

        return directory;
    }

    private static string SetNonWindowsPath(string applicationName, string home) => OperatingSystem.IsMacOS()
                    ? Path.Combine(home, "Library", "Application Support", applicationName)
                    : Path.Combine(home, ".config", applicationName);
}
