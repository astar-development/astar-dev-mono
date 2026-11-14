namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="WindowPresence"/> cases.</summary>
public static class WindowPresenceFactory
{
    /// <summary>Creates the <see cref="WindowPresence.InFront"/> case.</summary>
    public static WindowPresence CreateInFront() => new WindowPresence.InFront();

    /// <summary>Creates the <see cref="WindowPresence.NotInFront"/> case.</summary>
    public static WindowPresence CreateNotInFront() => new WindowPresence.NotInFront();
}
