namespace AStarDev.CloudSync.Classification;

/// <summary>Answers whether a word is a first name. Used for suggestions only; a match must never assign a Person term.</summary>
public interface IFirstNameList
{
    /// <summary>Checks a word against the first-name list, ignoring case and plural form. Never throws.</summary>
    /// <param name="word">The word to look up; null, blank and multi-word text give false.</param>
    /// <returns>True when the word is a first name.</returns>
    bool Contains(string? word);
}
