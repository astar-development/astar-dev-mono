using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Why a provider call failed. Expected failures are values, never exceptions.</summary>
public abstract record ProviderError
{
    private ProviderError()
    {
    }

    /// <summary>The account must sign in again (401 or an MSAL UI-required error).</summary>
    public sealed record AuthenticationRequired : ProviderError
    {
        internal AuthenticationRequired()
        {
        }
    }

    /// <summary>The provider throttled the account (429, 503) after the SDK used its retries.</summary>
    public sealed record Throttled : ProviderError
    {
        internal Throttled(Option<TimeSpan> retryAfter)
        {
            RetryAfter = retryAfter;
        }

        /// <summary>The provider's Retry-After, when it sent one.</summary>
        public Option<TimeSpan> RetryAfter { get; }
    }

    /// <summary>The item, its parent or the account does not exist.</summary>
    public sealed record NotFound : ProviderError
    {
        internal NotFound()
        {
        }
    }

    /// <summary>The provider refused access to the item (403).</summary>
    public sealed record AccessDenied : ProviderError
    {
        internal AccessDenied()
        {
        }
    }

    /// <summary>An item with the same name already exists.</summary>
    public sealed record NameConflict : ProviderError
    {
        internal NameConflict()
        {
        }
    }

    /// <summary>The change cursor is no longer valid (Graph 410); the engine runs a full resync.</summary>
    public sealed record CursorExpired : ProviderError
    {
        internal CursorExpired(ResyncKind kind, ChangeCursor restartFrom)
        {
            Kind = kind;
            RestartFrom = restartFrom;
        }

        /// <summary>What the resync must do.</summary>
        public ResyncKind Kind { get; }

        /// <summary>The cursor to restart the enumeration from.</summary>
        public ChangeCursor RestartFrom { get; }
    }

    /// <summary>A temporary failure: 5xx, timeout or network.</summary>
    public sealed record Transient : ProviderError
    {
        internal Transient()
        {
        }
    }

    /// <summary>Any other failure; the status and error code are logged.</summary>
    public sealed record Unexpected : ProviderError
    {
        internal Unexpected()
        {
        }
    }

    /// <summary>The account's provider does not offer this capability.</summary>
    public sealed record Unsupported : ProviderError
    {
        internal Unsupported()
        {
        }
    }

    /// <summary>The item changed since it was last seen (412); it goes to the conflict queue (#1405).</summary>
    public sealed record PreconditionFailed : ProviderError
    {
        internal PreconditionFailed()
        {
        }
    }

    /// <summary>The account has no space left (507).</summary>
    public sealed record QuotaExceeded : ProviderError
    {
        internal QuotaExceeded()
        {
        }
    }

    /// <summary>The file is larger than the provider accepts.</summary>
    public sealed record FileTooLarge : ProviderError
    {
        internal FileTooLarge()
        {
        }
    }

    /// <summary>The local file changed while it was being read, so the transfer was abandoned and the file is retried on the next pass.</summary>
    public sealed record SourceChanged : ProviderError
    {
        internal SourceChanged()
        {
        }
    }
}
