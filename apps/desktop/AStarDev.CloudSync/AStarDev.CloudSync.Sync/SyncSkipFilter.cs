namespace AStarDev.CloudSync.Sync;

/// <summary>The path rules that keep an item out of sync on both sides: anything inside a dot-prefixed folder, and the app's own <c>*.{guid}.download</c> temp files (GUID in <c>D</c> or <c>N</c> form). Dot-prefixed files such as <c>.gitignore</c> sync. A path alone cannot tell a folder from a file, so a dot-prefixed last segment is judged as a file; the local scan skips the folder itself. Local-only reasons (hidden attribute, symbolic links) are added by the local scan.</summary>
public static class SyncSkipFilter
{
    private const char Separator = '/';
    private const char DotPrefix = '.';
    private const string AppTempSuffix = ".download";

    /// <summary>Whether the path is never synced.</summary>
    /// <param name="path">The path relative to the sync root.</param>
    public static bool IsSkipped(RelativeSyncPath path)
    {
        var segments = (path.Value ?? string.Empty).Split(Separator);

        return segments[..^1].Any(folder => folder.StartsWith(DotPrefix)) || IsAppTempFile(segments[^1]);
    }

    private static bool IsAppTempFile(string name)
    {
        if (!name.EndsWith(AppTempSuffix, StringComparison.OrdinalIgnoreCase))
            return false;

        var withoutSuffix = name[..^AppTempSuffix.Length];
        var candidate = withoutSuffix[(withoutSuffix.LastIndexOf(DotPrefix) + 1)..];

        return Guid.TryParseExact(candidate, "D", out _) || Guid.TryParseExact(candidate, "N", out _);
    }
}
