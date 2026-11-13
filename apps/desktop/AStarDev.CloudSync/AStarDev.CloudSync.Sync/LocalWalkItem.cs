using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>One item the local walk reached: a file or folder to compare with the baseline, a path that clashes with a sibling that differs only in case, or a folder that could not be listed. Both of the last two have no info and are reported as unreadable.</summary>
internal sealed record LocalWalkItem
{
    private LocalWalkItem(RelativeSyncPath path, Option<IFileSystemInfo> info)
    {
        Path = path;
        Info = info;
    }

    internal RelativeSyncPath Path { get; }

    internal Option<IFileSystemInfo> Info { get; }

    internal static LocalWalkItem Found(RelativeSyncPath path, IFileSystemInfo info) => new(path, Option.Some(info));

    internal static LocalWalkItem Unlistable(RelativeSyncPath path) => new(path, Option.None<IFileSystemInfo>());

    internal static LocalWalkItem Clashing(RelativeSyncPath path) => new(path, Option.None<IFileSystemInfo>());
}
