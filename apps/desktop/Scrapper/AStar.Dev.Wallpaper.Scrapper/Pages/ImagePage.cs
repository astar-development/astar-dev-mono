using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class ImagePage(IPlaywrightService playwrightService, ScrapeConfiguration scrapeConfiguration, TagsToIgnoreCompletely tagsToIgnoreCompletely, TagsTextToIgnore tagsTextToIgnore)
{
    private IPage page = null!;

    public async Task<Result<ImagePageOutcome, ScrapeError>> GetImageFromPageAsync(string link, string categoryName)
        => (await Try.RunAsync(() => LoadOutcomeAsync(link, categoryName)).ConfigureAwait(false))
            .ToResult<ImagePageOutcome, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed(link, exception.Message));

    private async Task<ImagePageOutcome> LoadOutcomeAsync(string link, string categoryName)
    {
        await EnsurePageAsync().ConfigureAwait(false);
        _ = await page.GotoAsync(link).ConfigureAwait(false);

        var tagLocators = await page.Locator(".tagname").AllAsync().ConfigureAwait(false);
        var tagData = await Task.WhenAll(tagLocators.Select(GetTagsAsync)).ConfigureAwait(false);

        string initialDirectory = scrapeConfiguration.ScrapeDirectories.BaseSaveDirectory.CombinePath(categoryName.Replace(' ', '-'));
        var context = TagRuleContextFactory.Create(initialDirectory, scrapeConfiguration.ScrapeDirectories.BaseDirectoryFamous, tagsToIgnoreCompletely, tagsTextToIgnore);

        var outcome = TagRules.Evaluate(tagData, context);

        return await MapOutcomeAsync(outcome, tagData).ConfigureAwait(false);
    }

    private async Task<ImagePageOutcome> MapOutcomeAsync(TagOutcome outcome, IReadOnlyList<TagData> rawTags)
        => outcome switch
        {
            SkipImage skip => ImagePageOutcomeFactory.CreateSkippedImage(skip.Tags, rawTags),
            Accept accept => ImagePageOutcomeFactory.CreateScrapedImage(await GetImageSourceAsync().ConfigureAwait(false) ?? string.Empty, accept.DirectorySegments, accept.FilePrefix, accept.Tags, rawTags),
            _ => throw new InvalidOperationException("Unexpected tag outcome."),
        };

    private async Task<string?> GetImageSourceAsync()
    {
        var imageTag = page.Locator("#wallpaper");

        return await imageTag.GetAttributeAsync("src").ConfigureAwait(false);
    }

    private async Task EnsurePageAsync() => page ??= await playwrightService.ConfigurePlaywrightAsync().ConfigureAwait(false);

    private static async Task<TagData> GetTagsAsync(ILocator tag)
    {
        string textTask = await tag.InnerTextAsync().ConfigureAwait(false);
        string? attrTask = await tag.GetAttributeAsync("original-title").ConfigureAwait(false);

        return new TagData(textTask, attrTask);
    }
}
