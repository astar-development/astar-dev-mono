namespace AStarDev.CloudSync.Sync;

/// <summary>A message for the operating system's notification area (#1410). The title and the body are separate so a sender can pass them as separate arguments.</summary>
public sealed record DesktopNotification
{
    internal DesktopNotification(string title, string body)
    {
        Title = title;
        Body = body;
    }

    /// <summary>The headline.</summary>
    public string Title { get; }

    /// <summary>The text under the headline. It holds the account's display name and counts only.</summary>
    public string Body { get; }
}
