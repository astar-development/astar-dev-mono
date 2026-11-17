namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationFailure"/> cases.</summary>
public static class ClassificationFailureFactory
{
    /// <summary>Creates the NotFound case.</summary>
    public static ClassificationFailure CreateNotFound() => new ClassificationFailure.NotFound();
    /// <summary>Creates the FileNotFound case.</summary>
    public static ClassificationFailure CreateFileNotFound() => new ClassificationFailure.FileNotFound();
    /// <summary>Creates the Unavailable case.</summary>
    public static ClassificationFailure CreateUnavailable() => new ClassificationFailure.Unavailable();
    /// <summary>Creates the InvalidSeed case.</summary>
    public static ClassificationFailure CreateInvalidSeed() => new ClassificationFailure.InvalidSeed();
}
