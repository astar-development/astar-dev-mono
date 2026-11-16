namespace AStarDev.CloudSync.Sync;

/// <summary>Why an audit could not count (#1801).</summary>
public abstract record FileAuditFailure
{
    private FileAuditFailure()
    {
    }

    /// <summary>The account has no sync folder yet, or it is missing, unmounted or cannot be listed.</summary>
    public sealed record RootUnavailable : FileAuditFailure
    {
        internal RootUnavailable()
        {
        }
    }

    /// <summary>The stored folder selection could not be read, so the audit cannot tell which paths are ignored.</summary>
    public sealed record RulesUnavailable : FileAuditFailure
    {
        internal RulesUnavailable()
        {
        }
    }

    /// <summary>The provider could not be read; <see cref="Error"/> says why.</summary>
    public sealed record CloudUnavailable : FileAuditFailure
    {
        internal CloudUnavailable(ProviderError error)
        {
            Error = error;
        }

        /// <summary>The provider error.</summary>
        public ProviderError Error { get; }
    }
}
