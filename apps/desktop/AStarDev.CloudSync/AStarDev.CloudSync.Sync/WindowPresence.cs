namespace AStarDev.CloudSync.Sync;

/// <summary>Whether the user is looking at the main window (#1410). A notification is only worth showing when they are not.</summary>
public abstract record WindowPresence
{
    private WindowPresence()
    {
    }

    /// <summary>The window is visible and has focus.</summary>
    public sealed record InFront : WindowPresence
    {
        internal InFront()
        {
        }
    }

    /// <summary>The window is hidden, minimised or behind another window.</summary>
    public sealed record NotInFront : WindowPresence
    {
        internal NotInFront()
        {
        }
    }
}
