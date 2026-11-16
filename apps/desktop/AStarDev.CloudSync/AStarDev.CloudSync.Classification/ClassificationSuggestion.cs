namespace AStarDev.CloudSync.Classification;

/// <summary>A word found in file paths that is not yet a term, with the number of files it appears in.</summary>
public sealed record ClassificationSuggestion
{
    internal ClassificationSuggestion(string word, int fileCount, SuggestionHint hint)
    {
        Word = word;
        FileCount = fileCount;
        Hint = hint;
    }

    /// <summary>Gets the original lower-cased word, for display.</summary>
    public string Word { get; }

    /// <summary>Gets the number of distinct files whose path contains the word.</summary>
    public int FileCount { get; }

    /// <summary>Gets the hint the UI can show next to the word.</summary>
    public SuggestionHint Hint { get; }

    /// <inheritdoc />
    public override string ToString() => "ClassificationSuggestion(redacted)";
}
