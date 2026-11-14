namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DatabaseHealthFailure"/> values.</summary>
public static class DatabaseHealthFailureFactory
{
    /// <summary>Creates the <see cref="DatabaseHealthFailure.Damaged"/> case.</summary>
    public static DatabaseHealthFailure CreateDamaged() => new DatabaseHealthFailure.Damaged();

    /// <summary>Creates the <see cref="DatabaseHealthFailure.Unreadable"/> case.</summary>
    public static DatabaseHealthFailure CreateUnreadable() => new DatabaseHealthFailure.Unreadable();
}
