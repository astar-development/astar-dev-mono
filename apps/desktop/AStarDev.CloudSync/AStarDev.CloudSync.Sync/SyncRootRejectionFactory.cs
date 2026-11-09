using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncRootRejection"/> cases.</summary>
public static class SyncRootRejectionFactory
{
    /// <summary>Creates the <see cref="SyncRootRejection.SameAs"/> case.</summary>
    public static SyncRootRejection CreateSameAs(AccountId other) => new SyncRootRejection.SameAs(other);

    /// <summary>Creates the <see cref="SyncRootRejection.InsideOf"/> case.</summary>
    public static SyncRootRejection CreateInsideOf(AccountId other) => new SyncRootRejection.InsideOf(other);

    /// <summary>Creates the <see cref="SyncRootRejection.Contains"/> case.</summary>
    public static SyncRootRejection CreateContains(AccountId other) => new SyncRootRejection.Contains(other);

    /// <summary>Creates the <see cref="SyncRootRejection.NotAbsolute"/> case.</summary>
    public static SyncRootRejection CreateNotAbsolute() => new SyncRootRejection.NotAbsolute();

    /// <summary>Creates the <see cref="SyncRootRejection.NotWritable"/> case.</summary>
    public static SyncRootRejection CreateNotWritable() => new SyncRootRejection.NotWritable();

    /// <summary>Creates the <see cref="SyncRootRejection.Unavailable"/> case.</summary>
    public static SyncRootRejection CreateUnavailable() => new SyncRootRejection.Unavailable();

    /// <summary>Creates the <see cref="SyncRootRejection.Store"/> case.</summary>
    public static SyncRootRejection CreateStore(ErrorResponse error) => new SyncRootRejection.Store(error);
}
