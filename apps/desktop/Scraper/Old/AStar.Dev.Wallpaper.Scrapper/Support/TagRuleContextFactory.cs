using AStar.Dev.Guard.Clauses;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Factory methods for creating instances of <see cref="TagRuleContext" />.</summary>
public static class TagRuleContextFactory
{
    /// <summary>Creates a <see cref="TagRuleContext" />.</summary>
    public static TagRuleContext Create(string initialDirectory, string baseDirectoryFamous, TagsToIgnoreCompletely tagsToIgnoreCompletely, TagsTextToIgnore tagsTextToIgnore)
    {
        GuardAgainst.Null(initialDirectory);
        GuardAgainst.Null(baseDirectoryFamous);
        GuardAgainst.Null(tagsToIgnoreCompletely);
        GuardAgainst.Null(tagsTextToIgnore);

        return new(initialDirectory, baseDirectoryFamous, tagsToIgnoreCompletely, tagsTextToIgnore);
    }
}
