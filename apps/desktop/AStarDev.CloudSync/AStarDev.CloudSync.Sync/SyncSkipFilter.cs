namespace AStarDev.CloudSync.Sync;

/// <summary>The path rules that keep an item out of sync on both sides: any dot-prefixed folder or file, and the app's own <c>*.{guid}.download</c> temp files. Local-only reasons (hidden attribute, symbolic links) are added by the local scan.</summary>
public static class SyncSkipFilter
{
    /// <summary>Whether the path is never synced.</summary>
    /// <param name="path">The path relative to the sync root.</param>
    public static bool IsSkipped(RelativeSyncPath path) => throw new NotImplementedException($"{path}");
}
