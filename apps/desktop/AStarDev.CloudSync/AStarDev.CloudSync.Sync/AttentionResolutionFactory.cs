namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="AttentionResolution"/> cases.</summary>
public static class AttentionResolutionFactory
{
    /// <summary>Creates the <see cref="AttentionResolution.KeepLocal"/> case.</summary>
    public static AttentionResolution CreateKeepLocal() => new AttentionResolution.KeepLocal();

    /// <summary>Creates the <see cref="AttentionResolution.KeepCloud"/> case.</summary>
    public static AttentionResolution CreateKeepCloud() => new AttentionResolution.KeepCloud();

    /// <summary>Creates the <see cref="AttentionResolution.KeepBoth"/> case.</summary>
    public static AttentionResolution CreateKeepBoth() => new AttentionResolution.KeepBoth();

    /// <summary>Creates the <see cref="AttentionResolution.ConfirmDeletions"/> case.</summary>
    public static AttentionResolution CreateConfirmDeletions() => new AttentionResolution.ConfirmDeletions();

    /// <summary>Creates the <see cref="AttentionResolution.AutoCleared"/> case.</summary>
    public static AttentionResolution CreateAutoCleared() => new AttentionResolution.AutoCleared();

    /// <summary>Creates the <see cref="AttentionResolution.RestoreItems"/> case.</summary>
    public static AttentionResolution CreateRestoreItems() => new AttentionResolution.RestoreItems();
}
