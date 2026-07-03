namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Normalises a remote OneDrive path by stripping the fixed root segments and splitting the remainder into folder segments and filename stem.</summary>
public static class PathNormaliser
{
    private const int RootSegmentsToSkip = 7;

    /// <summary>Strips the first <see cref="RootSegmentsToSkip"/> path segments from <paramref name="remotePath"/>. Returns <see cref="string.Empty"/> when no meaningful segments remain.</summary>
    public static string StripRootPath(string remotePath)
    {
        if (string.IsNullOrEmpty(remotePath))
            return string.Empty;

        string[] segments = remotePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length <= RootSegmentsToSkip)
            return string.Empty;

        return string.Join('/', segments[RootSegmentsToSkip..]);
    }

    /// <summary>Returns all folder-name segments from <paramref name="strippedPath"/> (every segment except the last, which is the filename). Returns an empty list when input is empty or contains only a filename.</summary>
    public static IReadOnlyList<string> GetFolderSegments(string strippedPath)
    {
        if (string.IsNullOrEmpty(strippedPath))
            return [];

        string[] segments = strippedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length <= 1)
            return [];

        return segments[..^1];
    }

    /// <summary>Returns the filename stem (name without extension) of the last segment in <paramref name="strippedPath"/>. Returns <see cref="string.Empty"/> for empty input.</summary>
    public static string GetFilenameStem(string strippedPath)
    {
        if (string.IsNullOrEmpty(strippedPath))
            return string.Empty;

        string[] segments = strippedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return Path.GetFileNameWithoutExtension(segments[^1]);
    }
}
