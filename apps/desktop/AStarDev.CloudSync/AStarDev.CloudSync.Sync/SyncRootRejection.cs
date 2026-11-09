using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Why a folder cannot be an account's sync root (#1394).</summary>
public abstract record SyncRootRejection
{
    private SyncRootRejection()
    {
    }

    /// <summary>The folder is the same folder as another account's sync root.</summary>
    public sealed record SameAs : SyncRootRejection
    {
        internal SameAs(AccountId other)
        {
            Other = other;
        }

        /// <summary>The account whose root clashes.</summary>
        public AccountId Other { get; }
    }

    /// <summary>The folder is inside another account's sync root.</summary>
    public sealed record InsideOf : SyncRootRejection
    {
        internal InsideOf(AccountId other)
        {
            Other = other;
        }

        /// <summary>The account whose root clashes.</summary>
        public AccountId Other { get; }
    }

    /// <summary>The folder contains another account's sync root.</summary>
    public sealed record Contains : SyncRootRejection
    {
        internal Contains(AccountId other)
        {
            Other = other;
        }

        /// <summary>The account whose root clashes.</summary>
        public AccountId Other { get; }
    }

    /// <summary>The path is not an absolute path.</summary>
    public sealed record NotAbsolute : SyncRootRejection
    {
        internal NotAbsolute()
        {
        }
    }

    /// <summary>The folder cannot be written to.</summary>
    public sealed record NotWritable : SyncRootRejection
    {
        internal NotWritable()
        {
        }
    }

    /// <summary>The drive or mount holding the folder is missing.</summary>
    public sealed record Unavailable : SyncRootRejection
    {
        internal Unavailable()
        {
        }
    }

    /// <summary>The store failed to read or save the root.</summary>
    public sealed record Store : SyncRootRejection
    {
        internal Store(ErrorResponse error)
        {
            Error = error;
        }

        /// <summary>The store's error.</summary>
        public ErrorResponse Error { get; }
    }
}
