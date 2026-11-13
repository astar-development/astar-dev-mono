namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncRestartChoice"/> values.</summary>
public static class SyncRestartChoiceFactory
{
    /// <summary>Creates the <see cref="SyncRestartChoice.CancelAndRestart"/> case.</summary>
    public static SyncRestartChoice CreateCancelAndRestart() => new SyncRestartChoice.CancelAndRestart();

    /// <summary>Creates the <see cref="SyncRestartChoice.KeepCurrent"/> case.</summary>
    public static SyncRestartChoice CreateKeepCurrent() => new SyncRestartChoice.KeepCurrent();
}
