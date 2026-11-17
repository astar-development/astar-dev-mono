using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Matches the words in the file name, without its extension, against vocabulary terms.</summary>
public sealed class DictionaryTermSignal : IClassificationSignal
{
    /// <inheritdoc />
    public IReadOnlyList<ClassificationCandidate> Evaluate(RelativeSyncPath path, ClassificationVocabulary vocabulary) =>
        SignalCandidates.Create(PhraseMatcher.Match(ClassificationTokeniser.Tokenise(Path.GetFileNameWithoutExtension(path.Value.AsSpan())), vocabulary.PhraseIndex), vocabulary, SignalKindFactory.CreateVocabulary());
}
