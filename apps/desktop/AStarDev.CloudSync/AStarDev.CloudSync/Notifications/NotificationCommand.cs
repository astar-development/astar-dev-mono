namespace AStarDev.CloudSync.Notifications;

/// <summary>A program to run to show one notification (#1410). The text travels in the arguments or the environment, never in a shell string.</summary>
public sealed record NotificationCommand
{
    internal NotificationCommand(string program, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment)
    {
        Program = program;
        Arguments = arguments;
        Environment = environment;
    }

    /// <summary>The absolute path of the program.</summary>
    public string Program { get; }

    /// <summary>The arguments, one list item each.</summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>Extra environment variables for the program.</summary>
    public IReadOnlyDictionary<string, string> Environment { get; }
}
