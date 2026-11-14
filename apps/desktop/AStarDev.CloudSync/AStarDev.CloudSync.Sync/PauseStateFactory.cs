namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="PauseState"/> values.</summary>
public static class PauseStateFactory
{
    /// <summary>Creates the <see cref="PauseState.Active"/> case.</summary>
    public static PauseState CreateActive() => new PauseState.Active();

    /// <summary>Creates the <see cref="PauseState.Paused"/> case.</summary>
    public static PauseState CreatePaused() => new PauseState.Paused();
}
