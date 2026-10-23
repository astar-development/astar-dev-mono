namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Creates <see cref="ProcessCommand"/> instances.</summary>
public static class ProcessCommandFactory
{
    /// <summary>Creates a command with the given arguments.</summary>
    public static ProcessCommand Create(string fileName, params string[] arguments) => new(fileName, arguments);
}
