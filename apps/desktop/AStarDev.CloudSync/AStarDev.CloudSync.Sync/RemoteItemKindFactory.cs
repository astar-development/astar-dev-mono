using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteItemKind"/> cases.</summary>
public static class RemoteItemKindFactory
{
    /// <summary>Creates the <see cref="RemoteItemKind.File"/> case. A negative size becomes zero and a missing cTag an empty string.</summary>
    public static RemoteItemKind CreateFile(long size, Option<string> hash, string? cTag) => new RemoteItemKind.File(Math.Max(size, 0), hash, cTag ?? string.Empty);

    /// <summary>Creates the <see cref="RemoteItemKind.Folder"/> case.</summary>
    public static RemoteItemKind CreateFolder() => new RemoteItemKind.Folder();

    /// <summary>Creates the <see cref="RemoteItemKind.Deleted"/> case.</summary>
    public static RemoteItemKind CreateDeleted() => new RemoteItemKind.Deleted();
}
