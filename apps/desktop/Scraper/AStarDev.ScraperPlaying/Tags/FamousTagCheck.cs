using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.ScraperPlaying.Tags;

/// <summary>Decides whether a tag the application has not stored before is famous. Once a tag is stored its <c>IsFamous</c> flag, which the user can change, decides instead.</summary>
public static class FamousTagCheck
{
    /// <summary>Whether <paramref name="tag"/> is a person name: it is in one of the person categories and its name starts with an upper-case letter, which rules out descriptive tags such as "finger pointing".</summary>
    /// <param name="tag">The tag to check.</param>
    /// <param name="personCategories">The tag category names whose tags are person names, matched case-insensitively.</param>
    public static bool IsFamous(Tag tag, IReadOnlyList<string> personCategories)
        => tag.Name.Length > 0 && char.IsUpper(tag.Name[0]) && personCategories.Contains(tag.Category, StringComparer.OrdinalIgnoreCase);
}
