namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncBaselineKind"/> cases.</summary>
public static class SyncBaselineKindFactory
{
    /// <summary>Creates the <see cref="SyncBaselineKind.File"/> case. A negative size becomes zero and the time is converted to UTC.</summary>
    /// <param name="size">The size in bytes.</param>
    /// <param name="lastWriteUtc">The local last write time.</param>
    /// <param name="hash">The local SHA-256.</param>
    /// <param name="cTag">The remote content tag.</param>
    /// <param name="eTag">The remote entity tag.</param>
    public static SyncBaselineKind CreateFile(long size, DateTimeOffset lastWriteUtc, ContentHash hash, ContentTag cTag, EntityTag eTag) => new SyncBaselineKind.File(Math.Max(size, 0), lastWriteUtc.ToUniversalTime(), hash, cTag, eTag);

    /// <summary>Creates the <see cref="SyncBaselineKind.Folder"/> case.</summary>
    public static SyncBaselineKind CreateFolder() => new SyncBaselineKind.Folder();
}
