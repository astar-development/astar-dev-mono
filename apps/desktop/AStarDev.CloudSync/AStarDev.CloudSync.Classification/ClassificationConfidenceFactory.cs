namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationConfidence"/> cases.</summary>
public static class ClassificationConfidenceFactory
{
    /// <summary>Creates the High case.</summary>
    public static ClassificationConfidence CreateHigh() => new ClassificationConfidence.High();
    /// <summary>Creates the Low case.</summary>
    public static ClassificationConfidence CreateLow() => new ClassificationConfidence.Low();
}
