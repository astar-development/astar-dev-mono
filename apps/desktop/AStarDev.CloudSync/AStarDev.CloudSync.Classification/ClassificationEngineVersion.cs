namespace AStarDev.CloudSync.Classification;

/// <summary>The version of the classification engine: the tokeniser, the signals, the combiner and the embedded word lists. Bump <see cref="Current"/> whenever a change to any of them can give a file different automatic links, so the classification page offers a reclassify.</summary>
public static class ClassificationEngineVersion
{
    /// <summary>The engine version recorded with each completed reclassify run.</summary>
    public const string Current = "1";
}
