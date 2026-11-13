using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SideSnapshot"/> cases.</summary>
public static class SideSnapshotFactory
{
    /// <summary>Creates the <see cref="SideSnapshot.Absent"/> case.</summary>
    public static SideSnapshot CreateAbsent() => new SideSnapshot.Absent();

    /// <summary>Creates the <see cref="SideSnapshot.Present"/> case. A negative size is stored as zero.</summary>
    /// <param name="size">The size in bytes.</param>
    /// <param name="lastModified">When the item last changed on this side.</param>
    /// <param name="hash">The local content hash, when known.</param>
    /// <param name="tag">The cloud content tag, when known.</param>
    public static SideSnapshot CreatePresent(long size, DateTimeOffset lastModified, Option<ContentHash> hash, Option<ContentTag> tag) => new SideSnapshot.Present(Math.Max(size, 0), lastModified, hash, tag);
}
