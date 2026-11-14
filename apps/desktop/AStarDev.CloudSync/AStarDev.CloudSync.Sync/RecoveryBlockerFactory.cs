namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RecoveryBlocker"/> values.</summary>
public static class RecoveryBlockerFactory
{
    /// <summary>Creates the <see cref="RecoveryBlocker.SignInRequired"/> case.</summary>
    public static RecoveryBlocker CreateSignInRequired() => new RecoveryBlocker.SignInRequired();

    /// <summary>Creates the <see cref="RecoveryBlocker.SyncFolderMissing"/> case.</summary>
    public static RecoveryBlocker CreateSyncFolderMissing() => new RecoveryBlocker.SyncFolderMissing();

    /// <summary>Creates the <see cref="RecoveryBlocker.DatabaseUnreadable"/> case.</summary>
    public static RecoveryBlocker CreateDatabaseUnreadable() => new RecoveryBlocker.DatabaseUnreadable();
}
