namespace AStarDev.CloudSync.Classification;

/// <summary>Finds vocabulary terms in a token sequence, longest phrase first.</summary>
public static class PhraseMatcher
{
    /// <summary>Matches terms against the tokens.</summary>
    /// <param name="tokens">The tokens.</param>
    /// <param name="terms">The terms.</param>
    /// <returns>The matched terms.</returns>
    public static IReadOnlyList<ClassificationTerm> Match(IReadOnlyList<string> tokens, IReadOnlyList<ClassificationTerm> terms) => throw new NotImplementedException($"{tokens.Count}{terms.Count}");
}
