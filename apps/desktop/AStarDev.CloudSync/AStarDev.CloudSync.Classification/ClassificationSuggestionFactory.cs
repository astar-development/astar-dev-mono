namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationSuggestion"/> values.</summary>
public static class ClassificationSuggestionFactory
{
    /// <summary>Creates a suggestion, lower-casing the word and clamping a negative count to zero.</summary>
    /// <param name="word">The word.</param>
    /// <param name="fileCount">The number of files containing the word.</param>
    /// <param name="hint">The hint for the UI.</param>
    public static ClassificationSuggestion Create(string word, int fileCount, SuggestionHint hint) => new((word ?? string.Empty).Trim().ToLowerInvariant(), Math.Max(fileCount, 0), hint);
}
