using System.Text;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalRelativePath"/> values.</summary>
public static class LocalRelativePathFactory
{
    private const char Separator = '/';
    private const char Backslash = '\\';

    /// <summary>The sync root itself.</summary>
    public static LocalRelativePath Root { get; } = new(string.Empty);

    /// <summary>Creates a path. Backslashes become <c>/</c>, a trailing separator is removed and Unicode is composed (NFC). An empty path is the root.</summary>
    /// <param name="value">The relative path.</param>
    /// <returns>The path, or <see cref="LocalFolderError.InvalidPath"/> when it is absolute or has a <c>.</c> or <c>..</c> segment.</returns>
    public static Result<LocalRelativePath, LocalFolderError> Create(string? value)
    {
        var text = (value ?? string.Empty).Trim();
        var segments = text.Replace(Backslash, Separator).Normalize(NormalizationForm.FormC).Split(Separator, StringSplitOptions.RemoveEmptyEntries);

        return IsAbsolute(text) || segments.Any(segment => segment is "." or "..")
            ? Result.Failure<LocalRelativePath, LocalFolderError>(LocalFolderErrorFactory.CreateInvalidPath())
            : Result.Success<LocalRelativePath, LocalFolderError>(new LocalRelativePath(string.Join(Separator, segments)));
    }

    private static bool IsAbsolute(string text) => text.StartsWith(Separator) || text.StartsWith(Backslash) || (text.Length >= 2 && char.IsAsciiLetter(text[0]) && text[1] == ':');
}
