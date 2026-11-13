namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DeletionDirection"/> cases.</summary>
public static class DeletionDirectionFactory
{
    /// <summary>Creates the <see cref="DeletionDirection.DeleteLocal"/> case.</summary>
    public static DeletionDirection CreateDeleteLocal() => new DeletionDirection.DeleteLocal();

    /// <summary>Creates the <see cref="DeletionDirection.DeleteRemote"/> case.</summary>
    public static DeletionDirection CreateDeleteRemote() => new DeletionDirection.DeleteRemote();
}
