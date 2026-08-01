using AStar.Dev.Wallpaper.Scrapper.Repositories;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Pure classification rules for an image's tags, frozen from the historical <c>ImagePage.ProcessTheImageTagsAsync</c> behaviour.</summary>
public static class TagRules
{
    /// <summary>Evaluates <paramref name="tagData" /> against <paramref name="context" />, deciding whether the image should be skipped or accepted.</summary>
    public static TagOutcome Evaluate(IReadOnlyList<TagData> tagData, TagRuleContext context)
    {
        var tags = ExtractTagText(tagData);
        List<string> directorySegments = [context.InitialDirectory,];
        string filePrefix = string.Empty;

        foreach (var (tagText, tagToUse) in tagData)
        {
            if (tagToUse is null) continue;

            string trimmedTagToUse = tagToUse.Trim();

            if (IsOneOfTheImageTagsToExcludeCompletely(trimmedTagToUse, context) || IsOneOfTheImageTagsToExcludeCompletely(tagText, context))
                return TagOutcomeFactory.CreateSkipImage(tags);

            (filePrefix, directorySegments) = UpdateFilePrefixForModels(trimmedTagToUse, tagText, filePrefix, directorySegments, context);
            filePrefix = UpdateFilePrefixForVehicles(trimmedTagToUse, filePrefix, context);

            if (UpdateToTagIsNotRequired(trimmedTagToUse, tagText, filePrefix, context)) continue;

            filePrefix = string.Join("-", filePrefix, tagText.Replace(' ', '-')).ToLowerInvariant();
            directorySegments = [.. directorySegments, context.BaseDirectoryFamous,];
        }

        filePrefix = StripLeadingDash(filePrefix);

        return TagOutcomeFactory.CreateAccept(filePrefix, directorySegments, tags);
    }

    private static IReadOnlyList<string> ExtractTagText(IReadOnlyList<TagData> tagData)
        => [.. tagData.Select(tag => tag.Tag).Where(tag => !string.IsNullOrWhiteSpace(tag)),];

    private static string StripLeadingDash(string filePrefix)
        => filePrefix.StartsWith('-') ? filePrefix[1..] : filePrefix;

    private static bool IsOneOfTheImageTagsToExcludeCompletely(string tagText, TagRuleContext context)
        => context.TagsToIgnoreCompletely.Tags.Contains(tagText);

    private static bool IsWantedText(string tagText, TagRuleContext context)
        => !context.TagsTextToIgnore.Tags.Contains(tagText) && !tagText.StartsWith("model", StringComparison.OrdinalIgnoreCase);

    private static bool TagContains(string tagToUse, string contains)
        => tagToUse.Contains(contains, StringComparison.OrdinalIgnoreCase);

    private static bool IsPeopleTag(string tagToUse)
        => TagContains(tagToUse, "people > model")
           || TagContains(tagToUse, "people > porn")
           || TagContains(tagToUse, "people > actress")
           || TagContains(tagToUse, "people > actor")
           || TagContains(tagToUse, "people > singer");

    private static (string FilePrefix, List<string> DirectorySegments) UpdateFilePrefixForModels(string tagToUse, string tagText, string filePrefix, List<string> directorySegments, TagRuleContext context)
    {
        if (!IsPeopleTag(tagToUse)) return (filePrefix, directorySegments);

        if (!IsWantedText(tagText, context) || filePrefix.Contains(tagText)) return (filePrefix, directorySegments);

        if (directorySegments.Contains(tagText) || filePrefix.Contains(tagText, StringComparison.OrdinalIgnoreCase)) return (filePrefix, directorySegments);

        return (string.Join("-", filePrefix, tagText), directorySegments);
    }

    private static string UpdateFilePrefixForVehicles(string tagToUse, string filePrefix, TagRuleContext context)
    {
        if (!TagContains(tagToUse, "Vehicles > Cars & Motorcycles")) return filePrefix;

        return IsWantedFilePrefix(tagToUse, filePrefix, context) ? string.Join("-", filePrefix, tagToUse) : filePrefix;
    }

    private static bool IsWantedFilePrefix(string tagToUse, string filePrefix, TagRuleContext context)
        => IsWantedText(tagToUse, context)
           && !filePrefix.Contains(tagToUse)
           && !tagToUse.Equals("car", StringComparison.OrdinalIgnoreCase)
           && !TagContains(tagToUse, "cars");

    private static bool UpdateToTagIsNotRequired(string tagToUse, string tagText, string filePrefix, TagRuleContext context)
        => TagIsNotCelebEtc(tagToUse) || FilePrefixDoesNotNeedUpdating(tagText, filePrefix, context);

    private static bool TagIsNotCelebEtc(string tagToUse)
        => !TagContains(tagToUse, "celeb") && !TagContains(tagToUse, "singer") && !TagContains(tagToUse, "actress");

    private static bool FilePrefixDoesNotNeedUpdating(string tagText, string filePrefix, TagRuleContext context)
        => IsWantedText(tagText, context) || !filePrefix.Contains(tagText);
}
