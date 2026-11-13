namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalItemKind"/> cases.</summary>
public static class LocalItemKindFactory
{
    /// <summary>Creates the <see cref="LocalItemKind.File"/> case.</summary>
    public static LocalItemKind CreateFile() => new LocalItemKind.File();

    /// <summary>Creates the <see cref="LocalItemKind.Folder"/> case.</summary>
    public static LocalItemKind CreateFolder() => new LocalItemKind.Folder();
}
