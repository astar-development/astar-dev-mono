namespace AStarDev.CloudSync.Sync;

/// <summary>The path rules that keep an item out of sync on both sides: any dot-prefixed folder or file, and the app's own <c>*.{guid}.download</c> temp files. Local-only reasons (hidden attribute, symbolic links) are added by the local scan.</summary>
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

        return segments.Any(segment => segment.StartsWith(DotPrefix)) || IsAppTempFile(segments[^1]);
    }

    private static bool IsAppTempFile(string name)
    {
        if (!name.EndsWith(AppTempSuffix, StringComparison.OrdinalIgnoreCase))
            return false;

        var withoutSuffix = name[..^AppTempSuffix.Length];

        return Guid.TryParseExact(withoutSuffix[(withoutSuffix.LastIndexOf(DotPrefix) + 1)..], "D", out _);
    }
}
