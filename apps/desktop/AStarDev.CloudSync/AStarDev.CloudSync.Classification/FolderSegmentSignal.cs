using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Matches the words in each folder name of the path against vocabulary terms; the file name is ignored.</summary>
public sealed class FolderSegmentSignal : IClassificationSignal
{
    /// <inheritdoc />
    public IReadOnlyList<ClassificationCandidate> Evaluate(RelativeSyncPath path, ClassificationVocabulary vocabulary) =>
        SignalCandidates.Create((path.Value ?? string.Empty).Split('/').SkipLast(1).SelectMany(folder => PhraseMatcher.Match(ClassificationTokeniser.Tokenise(folder), vocabulary.PhraseIndex)), vocabulary, SignalKindFactory.CreateFolderName());
}
