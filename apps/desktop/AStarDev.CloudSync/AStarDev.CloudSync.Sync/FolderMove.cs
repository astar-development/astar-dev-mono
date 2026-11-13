namespace AStarDev.CloudSync.Sync;

/// <summary>A folder that moved or was renamed remotely: the path its rule has now and the path the folder has now (#1398).</summary>
public sealed record FolderMove
{
    internal FolderMove(LocalRelativePath oldPath, LocalRelativePath newPath)
    {
        OldPath = oldPath;
        NewPath = newPath;
    }

    /// <summary>The path the rule has now.</summary>
    public LocalRelativePath OldPath { get; }

    /// <summary>The path the folder has now.</summary>
    public LocalRelativePath NewPath { get; }
}
