namespace AStar.Dev.Sync.Engine.Features.SyncOrchestration;

/// <summary>Base discriminated union for all sync engine failures.</summary>
public abstract record SyncEngineError(string Message);

/// <summary>Returned when a sync is already in progress for the same account (SE-02, SE-06).</summary>
public sealed record SyncAlreadyRunningError() : SyncEngineError("A sync is already running for this account.");

/// <summary>Returned when the estimated download size exceeds available disk space + 10% buffer (EH-03).</summary>
public sealed record InsufficientDiskSpaceError(long AvailableBytes, long RequiredBytes) : SyncEngineError($"Insufficient disk space. Available: {AvailableBytes} bytes, required: {RequiredBytes} bytes.");

/// <summary>Returned when the persisted delta token has expired and the caller must confirm a full re-sync (SE-10).</summary>
public sealed record FullResyncRequiredError() : SyncEngineError("Delta token has expired. A full re-sync is required. Confirm to proceed.");

/// <summary>Returned when the interrupted-sync checkpoint is corrupt and a resume cannot be attempted (EH-06).</summary>
public sealed record ResumeFailedError() : SyncEngineError("Cannot resume interrupted sync — checkpoint state is corrupt.");

/// <summary>Returned when the local sync folder is not accessible (e.g. unmounted drive) (AM-11).</summary>
public sealed record LocalPathUnavailableError(string LocalPath) : SyncEngineError($"Local folder unavailable: {LocalPath}. Check your drive.");

/// <summary>Factory for <see cref="SyncEngineError"/> subtypes.</summary>
public static class SyncEngineErrorFactory
{
    /// <summary>Creates a <see cref="SyncAlreadyRunningError"/>.</summary>
    public static SyncAlreadyRunningError AlreadyRunning() => new();

    /// <summary>Creates an <see cref="InsufficientDiskSpaceError"/>.</summary>
    public static InsufficientDiskSpaceError InsufficientSpace(long availableBytes, long requiredBytes) => new(availableBytes, requiredBytes);

    /// <summary>Creates a <see cref="FullResyncRequiredError"/>.</summary>
    public static FullResyncRequiredError FullResyncRequired() => new();

    /// <summary>Creates a <see cref="ResumeFailedError"/>.</summary>
    public static ResumeFailedError ResumeFailed() => new();

    /// <summary>Creates a <see cref="LocalPathUnavailableError"/>.</summary>
    public static LocalPathUnavailableError LocalPathUnavailable(string localPath = "") => new(localPath);
}
