using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Matches the file extension against vocabulary terms such as "jpg" or ".jpg". The terms are indexed by extension once per vocabulary, so a lookup does not scan every term.</summary>
public sealed class ExtensionSignal : IClassificationSignal
{
    private static readonly ConditionalWeakTable<ClassificationVocabulary, Dictionary<string, List<ClassificationTerm>>> Indexes = [];

    /// <inheritdoc />
    public IReadOnlyList<ClassificationCandidate> Evaluate(RelativeSyncPath path, ClassificationVocabulary vocabulary)
    {
        var extension = Path.GetExtension(path.Value ?? string.Empty).TrimStart('.');

        return extension.Length == 0 || !Indexes.GetValue(vocabulary, Index).TryGetValue(extension, out var terms)
            ? []
            : SignalCandidates.Create(terms, vocabulary, SignalKindFactory.CreateExtension());
    }

    private static Dictionary<string, List<ClassificationTerm>> Index(ClassificationVocabulary vocabulary) =>
        vocabulary.Terms.GroupBy(term => term.Text.TrimStart('.'), StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
}
