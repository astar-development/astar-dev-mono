namespace AStarDev.CloudSync.Notifications;

/// <summary>Creates <see cref="NotificationCommand"/> values.</summary>
public static class NotificationCommandFactory
{
    /// <summary>Creates a command with no extra environment.</summary>
    /// <param name="program">The absolute path of the program.</param>
    /// <param name="arguments">The arguments.</param>
    public static NotificationCommand Create(string program, IReadOnlyList<string> arguments) => new(program, arguments, new Dictionary<string, string>());

    /// <summary>Creates a command with extra environment variables.</summary>
    /// <param name="program">The absolute path of the program.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="environment">The extra environment variables.</param>
    public static NotificationCommand Create(string program, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment) => new(program, arguments, environment);
}
