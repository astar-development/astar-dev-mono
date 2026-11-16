namespace AStarDev.CloudSync.Classification;

/// <summary>Answers whether a word is a common English word, so an unknown token can be judged as a possible proper word.</summary>
public interface IKnownWordList
{
    /// <summary>Checks a word against the common-word list, ignoring case and plural form. Never throws.</summary>
    /// <param name="word">The word to look up; null, blank and multi-word text give false.</param>
    /// <returns>True when the word is a common English word.</returns>
    bool Contains(string? word);
}
