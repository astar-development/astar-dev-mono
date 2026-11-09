using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ProviderError"/> cases.</summary>
public static class ProviderErrorFactory
{
    /// <summary>Creates the <see cref="ProviderError.AuthenticationRequired"/> case.</summary>
    public static ProviderError CreateAuthenticationRequired() => new ProviderError.AuthenticationRequired();

    /// <summary>Creates the <see cref="ProviderError.Throttled"/> case. A negative Retry-After becomes zero.</summary>
    public static ProviderError CreateThrottled(Option<TimeSpan> retryAfter) => new ProviderError.Throttled(retryAfter.Map(delay => delay < TimeSpan.Zero ? TimeSpan.Zero : delay));

    /// <summary>Creates the <see cref="ProviderError.NotFound"/> case.</summary>
    public static ProviderError CreateNotFound() => new ProviderError.NotFound();

    /// <summary>Creates the <see cref="ProviderError.AccessDenied"/> case.</summary>
    public static ProviderError CreateAccessDenied() => new ProviderError.AccessDenied();

    /// <summary>Creates the <see cref="ProviderError.NameConflict"/> case.</summary>
    public static ProviderError CreateNameConflict() => new ProviderError.NameConflict();

    /// <summary>Creates the <see cref="ProviderError.CursorExpired"/> case.</summary>
    public static ProviderError CreateCursorExpired(ResyncKind kind, ChangeCursor restartFrom) => new ProviderError.CursorExpired(kind, restartFrom);

    /// <summary>Creates the <see cref="ProviderError.Transient"/> case.</summary>
    public static ProviderError CreateTransient() => new ProviderError.Transient();

    /// <summary>Creates the <see cref="ProviderError.Unexpected"/> case.</summary>
    public static ProviderError CreateUnexpected() => new ProviderError.Unexpected();

    /// <summary>Creates the <see cref="ProviderError.Unsupported"/> case.</summary>
    public static ProviderError CreateUnsupported() => new ProviderError.Unsupported();

    /// <summary>Creates the <see cref="ProviderError.PreconditionFailed"/> case.</summary>
    public static ProviderError CreatePreconditionFailed() => new ProviderError.PreconditionFailed();

    /// <summary>Creates the <see cref="ProviderError.QuotaExceeded"/> case.</summary>
    public static ProviderError CreateQuotaExceeded() => new ProviderError.QuotaExceeded();

    /// <summary>Creates the <see cref="ProviderError.FileTooLarge"/> case.</summary>
    public static ProviderError CreateFileTooLarge() => new ProviderError.FileTooLarge();
}
