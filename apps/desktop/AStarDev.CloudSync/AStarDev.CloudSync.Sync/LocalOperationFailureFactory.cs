namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalOperationFailure"/> values.</summary>
public static class LocalOperationFailureFactory
{
    /// <summary>Creates the <see cref="LocalOperationFailure.TargetChanged"/> case.</summary>
    public static LocalOperationFailure CreateTargetChanged() => new LocalOperationFailure.TargetChanged();

    /// <summary>Creates the <see cref="LocalOperationFailure.AccessDenied"/> case.</summary>
    public static LocalOperationFailure CreateAccessDenied() => new LocalOperationFailure.AccessDenied();

    /// <summary>Creates the <see cref="LocalOperationFailure.Locked"/> case.</summary>
    public static LocalOperationFailure CreateLocked() => new LocalOperationFailure.Locked();

    /// <summary>Creates the <see cref="LocalOperationFailure.InsufficientDiskSpace"/> case.</summary>
    public static LocalOperationFailure CreateInsufficientDiskSpace() => new LocalOperationFailure.InsufficientDiskSpace();

    /// <summary>Creates the <see cref="LocalOperationFailure.PathTooLong"/> case.</summary>
    public static LocalOperationFailure CreatePathTooLong() => new LocalOperationFailure.PathTooLong();

    /// <summary>Creates the <see cref="LocalOperationFailure.Unexpected"/> case.</summary>
    public static LocalOperationFailure CreateUnexpected() => new LocalOperationFailure.Unexpected();
}
