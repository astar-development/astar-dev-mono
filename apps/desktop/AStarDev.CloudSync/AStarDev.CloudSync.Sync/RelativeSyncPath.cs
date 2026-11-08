using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>A file or folder path relative to an account's sync root, with <c>/</c> separators, used on both the local and the remote side. Compared ignoring case by <see cref="RelativeSyncPathComparer"/>.</summary>
[StrongType(typeof(string))]
public partial record struct RelativeSyncPath
{
    /// <summary>Returns a redacted form, so the path never reaches logs by accident.</summary>
    public override string ToString() => "RelativeSyncPath(redacted)";
}
