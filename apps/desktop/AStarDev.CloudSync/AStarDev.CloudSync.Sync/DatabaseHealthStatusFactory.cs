namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DatabaseHealthStatus"/> values.</summary>
public static class DatabaseHealthStatusFactory
{
    /// <summary>Creates the <see cref="DatabaseHealthStatus.NotChecked"/> case.</summary>
    public static DatabaseHealthStatus CreateNotChecked() => new DatabaseHealthStatus.NotChecked();

    /// <summary>Creates the <see cref="DatabaseHealthStatus.Healthy"/> case.</summary>
    public static DatabaseHealthStatus CreateHealthy() => new DatabaseHealthStatus.Healthy();

    /// <summary>Creates the <see cref="DatabaseHealthStatus.Failed"/> case.</summary>
    /// <param name="failure">Why the check failed.</param>
    public static DatabaseHealthStatus CreateFailed(DatabaseHealthFailure failure) => new DatabaseHealthStatus.Failed(failure);
}
