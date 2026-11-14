namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Whether the user asked the app to quit (#1409). While it is <see cref="Quitting"/> the window really closes instead of hiding.</summary>
public abstract record ShutdownState
{
    private ShutdownState()
    {
    }

    /// <summary>No quit was requested.</summary>
    public sealed record Running : ShutdownState
    {
        internal Running()
        {
        }
    }

    /// <summary>A quit was requested.</summary>
    public sealed record Quitting : ShutdownState
    {
        internal Quitting()
        {
        }
    }
}
