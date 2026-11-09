using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="AccountRemovalFailure"/> cases.</summary>
public static class AccountRemovalFailureFactory
{
    /// <summary>Creates the not-found case.</summary>
    public static AccountRemovalFailure CreateNotFound() => new AccountRemovalFailure.NotFound();

    /// <summary>Creates the sync-still-running case.</summary>
    public static AccountRemovalFailure CreateSyncStillRunning() => new AccountRemovalFailure.SyncStillRunning();

    /// <summary>Creates the store case.</summary>
    public static AccountRemovalFailure CreateStore(ErrorResponse error) => new AccountRemovalFailure.Store(error);

    /// <summary>Creates the provider case.</summary>
    public static AccountRemovalFailure CreateProvider(ProviderError error) => new AccountRemovalFailure.Provider(error);
}
