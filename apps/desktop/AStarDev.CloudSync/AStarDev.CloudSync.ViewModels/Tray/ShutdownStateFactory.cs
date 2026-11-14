namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Creates <see cref="ShutdownState"/> cases.</summary>
public static class ShutdownStateFactory
{
    /// <summary>Creates the <see cref="ShutdownState.Running"/> case.</summary>
    public static ShutdownState CreateRunning() => new ShutdownState.Running();

    /// <summary>Creates the <see cref="ShutdownState.Quitting"/> case.</summary>
    public static ShutdownState CreateQuitting() => new ShutdownState.Quitting();
}
