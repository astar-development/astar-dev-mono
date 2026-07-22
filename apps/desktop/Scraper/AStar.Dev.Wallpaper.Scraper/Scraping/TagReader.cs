using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the tags shown on a wallpaper detail page.
/// </summary>
public sealed class TagReader : ITagReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TagData>> ReadAsync(IPage page, CancellationToken cancellationToken)
    {
        var tagLocators = await page.Locator(".tagname").AllAsync().ConfigureAwait(false);
        var tags = await Task.WhenAll(tagLocators.Select(ReadTagAsync)).ConfigureAwait(false);

        return tags;
    }

    private static async Task<TagData> ReadTagAsync(ILocator tag)
    {
        string text = await tag.InnerTextAsync().ConfigureAwait(false);
        string? category = await tag.GetAttributeAsync("original-title").ConfigureAwait(false);

        return TagDataFactory.Create(text, category);
    }
}
