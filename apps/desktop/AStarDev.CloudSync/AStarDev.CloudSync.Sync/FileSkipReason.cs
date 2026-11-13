namespace AStarDev.CloudSync.Sync;

/// <summary>Why a local file is left out of the scan (#1399). Dot-prefixed files such as <c>.gitignore</c> sync; only dot-prefixed folders are skipped (#1459).</summary>
public abstract record FileSkipReason
{
    private FileSkipReason()
    {
    }

    /// <summary>The file has the hidden attribute.</summary>
    public sealed record Hidden : FileSkipReason
    {
        internal Hidden()
        {
        }
    }

    /// <summary>The file is one of the app's own <c>*.{guid}.download</c> temp files.</summary>
    public sealed record Temporary : FileSkipReason
    {
        internal Temporary()
        {
        }
    }

    /// <summary>The file is an Office lock file (<c>~$*</c>).</summary>
    public sealed record OfficeLock : FileSkipReason
    {
        internal OfficeLock()
        {
        }
    }

    /// <summary>The file is an operating system file: <c>desktop.ini</c> or <c>Thumbs.db</c>.</summary>
    public sealed record SystemFile : FileSkipReason
    {
        internal SystemFile()
        {
        }
    }

    /// <summary>The file is a symbolic link, which is not followed.</summary>
    public sealed record SymbolicLink : FileSkipReason
    {
        internal SymbolicLink()
        {
        }
    }
}
