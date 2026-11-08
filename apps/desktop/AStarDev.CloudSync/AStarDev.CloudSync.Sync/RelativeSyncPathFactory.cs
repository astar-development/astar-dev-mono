using System.Text;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RelativeSyncPath"/> values.</summary>
public static class RelativeSyncPathFactory
{
    private const char Separator = '/';
    private const string InvalidPathError = "A sync path must be a non-empty path inside the sync root.";

    /// <summary>Creates a path. Backslashes become <c>/</c>, leading or trailing separators are removed and Unicode is composed (NFC), so a macOS decomposed name matches the remote one.</summary>
    /// <param name="value">The relative path.</param>
    /// <returns>The path, or an error when it is empty or contains a <c>.</c> or <c>..</c> segment.</returns>
    public static Result<RelativeSyncPath, ErrorResponse> Create(string? value)
    {
        var normalised = (value ?? string.Empty).Trim().Replace('\\', Separator).Trim(Separator).Normalize(NormalizationForm.FormC);

        return normalised.Length == 0 || normalised.Split(Separator).Any(IsInvalidSegment)
            ? new Fail<RelativeSyncPath, ErrorResponse>(new ErrorResponse(InvalidPathError))
            : new Ok<RelativeSyncPath, ErrorResponse>(new RelativeSyncPath(normalised));
    }

    private static bool IsInvalidSegment(string segment) => segment.Length == 0 || segment is "." or "..";
}
