using System.Reflection;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Serves the common-word and first-name lists embedded as text files. Entries are folded with <see cref="ClassificationTokeniser"/> once, on first use, so lookups compare match keys on both sides.</summary>
/// <param name="assembly">The assembly holding the embedded list files.</param>
/// <param name="resourcePrefix">The manifest resource name prefix of the list files.</param>
public sealed class EmbeddedWordLists(Assembly assembly, string resourcePrefix) : IKnownWordList, IFirstNameList
{
    private readonly Lazy<HashSet<string>> knownWords = new(() => Load(assembly, resourcePrefix + "common-words.txt"), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Lazy<HashSet<string>> firstNames = new(() => Load(assembly, resourcePrefix + "first-names.txt"), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <inheritdoc />
    bool IKnownWordList.Contains(string? word) => Lookup(knownWords, word);

    /// <inheritdoc />
    bool IFirstNameList.Contains(string? word) => Lookup(firstNames, word);

    private static bool Lookup(Lazy<HashSet<string>> list, string? word)
    {
        var tokens = ClassificationTokeniser.Tokenise(word);

        return tokens.Count == 1 && Try.Run(() => list.Value).Match(entries => entries.Contains(tokens[0]), _ => false);
    }

    private static HashSet<string> Load(Assembly source, string resourceName)
    {
        using var stream = source.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException("List file missing.");
        using var reader = new StreamReader(stream);

        return [.. reader.ReadToEnd().Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && line[0] != '#').SelectMany(line => ClassificationTokeniser.Tokenise(line))];
    }
}
