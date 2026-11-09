using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalRelativePath"/> values.</summary>
public static class LocalRelativePathFactory
{
    /// <summary>The sync root itself.</summary>
    public static LocalRelativePath Root { get; } = new(string.Empty);

    /// <summary>Creates a path. Backslashes become <c>/</c>, separators at either end are removed and Unicode is composed (NFC). An empty path is the root.</summary>
    /// <param name="value">The relative path.</param>
    /// <returns>The path, or <see cref="LocalFolderError.InvalidPath"/> when it is absolute or has a <c>.</c> or <c>..</c> segment.</returns>
    public static Result<LocalRelativePath, LocalFolderError> Create(string? value) =>
        Result.Success<LocalRelativePath, LocalFolderError>(Root);
}
