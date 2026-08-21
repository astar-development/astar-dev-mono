using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Localization;
using Microsoft.Playwright;

namespace AStarDev.WallpaperScraper.Services;

public sealed class PageProcessor(ILocalizationService localizationService, IPage page) : IPageProcessor
{
    /// <inheritdoc/>
    public async Task<Exceptional<PageResult>> ProcessPageAsync(IProgress<string> progress, Uri pageUrl, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.PageProcessor.Started"));

        var content = await page.GotoAsync(pageUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

        if (content is null || content.Status != 200)
        {
            return new OperationException($"Failed to load page content for: {pageUrl} with status message: {content?.StatusText}");
        }

        string pageText = await content.TextAsync();

        return new PageSuccess(pageText ?? "<html>Failed page content</html>", pageUrl);
    }
}
