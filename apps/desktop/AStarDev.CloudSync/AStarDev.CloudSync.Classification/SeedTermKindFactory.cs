namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="SeedTermKind"/> cases.</summary>
public static class SeedTermKindFactory
{
    /// <summary>Creates the Word case.</summary>
    public static SeedTermKind CreateWord() => new SeedTermKind.Word();

    /// <summary>Creates the Extension case.</summary>
    public static SeedTermKind CreateExtension() => new SeedTermKind.Extension();

    /// <summary>Creates the Folder case.</summary>
    public static SeedTermKind CreateFolder() => new SeedTermKind.Folder();
}
