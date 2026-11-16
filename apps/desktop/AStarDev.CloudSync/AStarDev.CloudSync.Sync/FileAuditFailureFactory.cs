namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FileAuditFailure"/> cases.</summary>
public static class FileAuditFailureFactory
{
    /// <summary>Creates the <see cref="FileAuditFailure.RootUnavailable"/> case.</summary>
    public static FileAuditFailure CreateRootUnavailable() => new FileAuditFailure.RootUnavailable();

    /// <summary>Creates the <see cref="FileAuditFailure.RulesUnavailable"/> case.</summary>
    public static FileAuditFailure CreateRulesUnavailable() => new FileAuditFailure.RulesUnavailable();

    /// <summary>Creates the <see cref="FileAuditFailure.CloudUnavailable"/> case.</summary>
    /// <param name="error">The provider error.</param>
    public static FileAuditFailure CreateCloudUnavailable(ProviderError error) => new FileAuditFailure.CloudUnavailable(error);
}
