namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ReauthFailure"/> cases.</summary>
public static class ReauthFailureFactory
{
    /// <summary>Creates the <see cref="ReauthFailure.Cancelled"/> case.</summary>
    public static ReauthFailure CreateCancelled() => new ReauthFailure.Cancelled();

    /// <summary>Creates the <see cref="ReauthFailure.DifferentAccount"/> case.</summary>
    public static ReauthFailure CreateDifferentAccount() => new ReauthFailure.DifferentAccount();

    /// <summary>Creates the <see cref="ReauthFailure.SyncStillRunning"/> case.</summary>
    public static ReauthFailure CreateSyncStillRunning() => new ReauthFailure.SyncStillRunning();

    /// <summary>Creates the <see cref="ReauthFailure.Provider"/> case.</summary>
    public static ReauthFailure CreateProvider(ProviderError error) => new ReauthFailure.Provider(error);
}
