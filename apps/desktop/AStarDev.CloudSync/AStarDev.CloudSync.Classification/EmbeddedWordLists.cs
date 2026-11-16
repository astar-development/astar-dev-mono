using System.Reflection;

namespace AStarDev.CloudSync.Classification;

/// <summary>Serves the common-word and first-name lists embedded as text files. Entries are folded with <see cref="ClassificationTokeniser"/> once, on first use, so lookups compare match keys on both sides.</summary>
/// <param name="assembly">The assembly holding the embedded list files.</param>
/// <param name="resourcePrefix">The manifest resource name prefix of the list files.</param>
public sealed class EmbeddedWordLists(Assembly assembly, string resourcePrefix) : IKnownWordList, IFirstNameList
{
    /// <inheritdoc />
    bool IKnownWordList.Contains(string? word) => throw new NotImplementedException($"{assembly}{resourcePrefix}{word}");

    /// <inheritdoc />
    bool IFirstNameList.Contains(string? word) => throw new NotImplementedException($"{word}");
}
