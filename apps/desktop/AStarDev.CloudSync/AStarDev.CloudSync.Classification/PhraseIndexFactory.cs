namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="PhraseIndex"/> values.</summary>
public static class PhraseIndexFactory
{
    /// <summary>Tokenises every term once and indexes it by phrase. A term whose text has no tokens is indexed under an empty phrase and never matches.</summary>
    /// <param name="terms">The terms to index.</param>
    public static PhraseIndex Create(IReadOnlyList<ClassificationTerm> terms) => throw new NotImplementedException($"{nameof(terms)}");
}
