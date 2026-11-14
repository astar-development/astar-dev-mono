namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ResolutionFailure"/> cases.</summary>
public static class ResolutionFailureFactory
{
    /// <summary>Creates the <see cref="ResolutionFailure.NotAllowed"/> case.</summary>
    /// <param name="reason">The reason.</param>
    public static ResolutionFailure CreateNotAllowed(ResolutionNotAllowed reason) => new ResolutionFailure.NotAllowed(reason);

    /// <summary>Creates the <see cref="ResolutionFailure.NotOpen"/> case.</summary>
    public static ResolutionFailure CreateNotOpen() => new ResolutionFailure.NotOpen();

    /// <summary>Creates the <see cref="ResolutionFailure.Unsupported"/> case.</summary>
    public static ResolutionFailure CreateUnsupported() => new ResolutionFailure.Unsupported();

    /// <summary>Creates the <see cref="ResolutionFailure.LocalFileMissing"/> case.</summary>
    public static ResolutionFailure CreateLocalFileMissing() => new ResolutionFailure.LocalFileMissing();

    /// <summary>Creates the <see cref="ResolutionFailure.RemoteFileMissing"/> case.</summary>
    public static ResolutionFailure CreateRemoteFileMissing() => new ResolutionFailure.RemoteFileMissing();

    /// <summary>Creates the <see cref="ResolutionFailure.LocalChanged"/> case.</summary>
    public static ResolutionFailure CreateLocalChanged() => new ResolutionFailure.LocalChanged();

    /// <summary>Creates the <see cref="ResolutionFailure.RemoteChanged"/> case.</summary>
    public static ResolutionFailure CreateRemoteChanged() => new ResolutionFailure.RemoteChanged();

    /// <summary>Creates the <see cref="ResolutionFailure.Transfer"/> case.</summary>
    /// <param name="error">What the provider reported.</param>
    public static ResolutionFailure CreateTransfer(ProviderError error) => new ResolutionFailure.Transfer(error);

    /// <summary>Creates the <see cref="ResolutionFailure.Local"/> case.</summary>
    /// <param name="failure">What the file system reported.</param>
    public static ResolutionFailure CreateLocal(LocalOperationFailure failure) => new ResolutionFailure.Local(failure);

    /// <summary>Creates the <see cref="ResolutionFailure.NotRecorded"/> case.</summary>
    public static ResolutionFailure CreateNotRecorded() => new ResolutionFailure.NotRecorded();

    /// <summary>Creates the <see cref="ResolutionFailure.Unexpected"/> case.</summary>
    public static ResolutionFailure CreateUnexpected() => new ResolutionFailure.Unexpected();
}
