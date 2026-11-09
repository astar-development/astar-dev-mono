using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Why a local folder listing failed (#1395).</summary>
public abstract record LocalFolderError
{
    private LocalFolderError()
    {
    }

    /// <summary>The account has no sync root, or its folder is missing or its drive is unmounted.</summary>
    public sealed record RootUnavailable : LocalFolderError
    {
        internal RootUnavailable()
        {
        }
    }

    /// <summary>The folder being listed no longer exists.</summary>
    public sealed record NotFound : LocalFolderError
    {
        internal NotFound()
        {
        }
    }

    /// <summary>The folder being listed cannot be read.</summary>
    public sealed record AccessDenied : LocalFolderError
    {
        internal AccessDenied()
        {
        }
    }

    /// <summary>The path is absolute or reaches outside the sync root.</summary>
    public sealed record InvalidPath : LocalFolderError
    {
        internal InvalidPath()
        {
        }
    }

    /// <summary>The listing failed for another reason.</summary>
    public sealed record Unexpected : LocalFolderError
    {
        internal Unexpected(ErrorResponse error)
        {
            Error = error;
        }

        /// <summary>The Error.</summary>
        public ErrorResponse Error { get; }
    }
}
