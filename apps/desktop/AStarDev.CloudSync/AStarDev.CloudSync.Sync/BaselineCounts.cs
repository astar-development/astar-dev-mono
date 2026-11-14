namespace AStarDev.CloudSync.Sync;

/// <summary>How many folders and files an account's sync baseline holds (#1565).</summary>
public sealed record BaselineCounts
{
    internal BaselineCounts(int folders, int files)
    {
        Folders = folders;
        Files = files;
    }

    /// <summary>The folders in the baseline.</summary>
    public int Folders { get; }

    /// <summary>The files in the baseline.</summary>
    public int Files { get; }
}
