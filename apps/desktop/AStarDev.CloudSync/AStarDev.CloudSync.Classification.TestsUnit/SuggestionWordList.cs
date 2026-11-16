namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class SuggestionWordList(params string[] words) : IKnownWordList, IFirstNameList
{
    private readonly HashSet<string> entries = new(words, StringComparer.Ordinal);

    public bool Contains(string? word) => word is not null && entries.Contains(word);
}
