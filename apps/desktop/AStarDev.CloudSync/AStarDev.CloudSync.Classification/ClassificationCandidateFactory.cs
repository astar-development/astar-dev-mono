namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationCandidate"/> values.</summary>
public static class ClassificationCandidateFactory
{
    /// <summary>Creates a candidate.</summary>
    /// <param name="term">The term.</param>
    /// <param name="category">Its category.</param>
    /// <param name="signal">The signal that found it.</param>
    /// <param name="confidence">How sure the signal is.</param>
    public static ClassificationCandidate Create(ClassificationTerm term, ClassificationCategory category, SignalKind signal, ClassificationConfidence confidence) => new(term, category, signal, confidence);
}
