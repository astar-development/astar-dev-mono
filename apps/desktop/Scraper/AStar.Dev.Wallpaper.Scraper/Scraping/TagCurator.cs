namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Filters a wallpaper's raw tags against the models-to-ignore and tags-to-ignore lists.
/// </summary>
public static class TagCurator
{
    /// <summary>
    ///     Curates the supplied tags, dropping any that match either ignore list.
    /// </summary>
    /// <param name="tags">The raw tags read from the wallpaper page.</param>
    /// <param name="modelsToIgnore">Model names whose wallpapers should not be saved.</param>
    /// <param name="tagsToIgnore">Tag names that should be dropped from a wallpaper's tag list.</param>
    public static TagCuration Curate(IReadOnlyList<TagData> tags, IReadOnlyList<string> modelsToIgnore, IReadOnlyList<string> tagsToIgnore)
    {
        var outcomes = tags
            .Select(NormalizeModelSuffix)
            .OfType<TagData>()
            .Select(tag => Evaluate(tag, modelsToIgnore, tagsToIgnore))
            .ToList();

        var kept = outcomes.Where(outcome => outcome.Kept).Select(outcome => outcome.Tag).ToList();
        var messages = outcomes.SelectMany(outcome => outcome.Messages).ToList();

        return new TagCuration(kept, messages);
    }

    private static TagData? NormalizeModelSuffix(TagData tag)
    {
        if (tag.Tag.Equals("model", StringComparison.OrdinalIgnoreCase)) return null;

        if (tag.Tag.EndsWith(" model", StringComparison.OrdinalIgnoreCase)) return tag with { Tag = tag.Tag[..^" model".Length] };

        return tag;
    }

    private static TagCurationOutcome Evaluate(TagData tag, IReadOnlyList<string> modelsToIgnore, IReadOnlyList<string> tagsToIgnore)
    {
        string foundMessage = $"Found tag: {tag.Tag}, category: {tag.Category}, isFamous: {tag.IsFamous}, isInternet: {tag.IsInternet}";

        if (ModelOrTagToIgnore(modelsToIgnore, tagsToIgnore, tag)) return new TagCurationOutcome(false, tag, [foundMessage]);

        return new TagCurationOutcome(true, tag, [foundMessage, $"Tag: {tag.Tag} is not in the tagsToIgnore list, added to the list of tags to save to the database"]);
    }

    private static bool ModelOrTagToIgnore(IReadOnlyList<string> modelsToIgnore, IReadOnlyList<string> tagsToIgnore, TagData tag)
        => modelsToIgnore.Any(model => TagCheck(tag.Tag, model)) || tagsToIgnore.Any(ignoredTag => TagCheck(tag.Tag, ignoredTag));

    private static bool TagCheck(string category, string contains)
        => category.Contains(contains, StringComparison.OrdinalIgnoreCase);

    private sealed record TagCurationOutcome(bool Kept, TagData Tag, IReadOnlyList<string> Messages);
}
