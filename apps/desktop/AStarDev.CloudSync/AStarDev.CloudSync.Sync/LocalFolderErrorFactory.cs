using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalFolderError"/> cases.</summary>
public static class LocalFolderErrorFactory
{
    /// <summary>Creates the <see cref="LocalFolderError.RootUnavailable"/> case.</summary>
    public static LocalFolderError CreateRootUnavailable() => new LocalFolderError.RootUnavailable();

    /// <summary>Creates the <see cref="LocalFolderError.NotFound"/> case.</summary>
    public static LocalFolderError CreateNotFound() => new LocalFolderError.NotFound();

    /// <summary>Creates the <see cref="LocalFolderError.AccessDenied"/> case.</summary>
    public static LocalFolderError CreateAccessDenied() => new LocalFolderError.AccessDenied();

    /// <summary>Creates the <see cref="LocalFolderError.InvalidPath"/> case.</summary>
    public static LocalFolderError CreateInvalidPath() => new LocalFolderError.InvalidPath();

    /// <summary>Creates the <see cref="LocalFolderError.Unexpected"/> case.</summary>
    public static LocalFolderError CreateUnexpected(ErrorResponse error) => new LocalFolderError.Unexpected(error);
}
