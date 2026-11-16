using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Matches the file extension against vocabulary terms such as "jpg" or ".jpg".</summary>
public sealed class ExtensionSignal : IClassificationSignal
{
    /// <inheritdoc />
    public IReadOnlyList<ClassificationCandidate> Evaluate(RelativeSyncPath path, ClassificationVocabulary vocabulary)
    {
        var extension = Path.GetExtension(path.Value ?? string.Empty).TrimStart('.');

        return extension.Length == 0
            ? []
            : SignalCandidates.Create(vocabulary.Terms.Where(term => string.Equals(term.Text.TrimStart('.'), extension, StringComparison.OrdinalIgnoreCase)), vocabulary, SignalKindFactory.CreateExtension());
    }
}
