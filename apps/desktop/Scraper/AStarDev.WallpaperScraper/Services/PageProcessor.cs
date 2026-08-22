using AStar.Dev.FunctionalParadigm;
using AStarDev.SourceGeneratorAttributes;
using AStarDev.WallpaperScraper.Localization;
using Microsoft.Playwright;

namespace AStarDev.WallpaperScraper.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public sealed class PageProcessor(ILocalizationService localizationService) : IPageProcessor
{
    /// <inheritdoc/>
    public async Task<Exceptional<PageResult>> ProcessPageAsync(IProgress<string> progress, Uri pageUrl, IPage page, CancellationToken cancellationToken)
        => await Try.RunAsync<PageResult>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress.Report(localizationService.GetLocal("Scraper.PageProcessor.Started"));

                var content = await page.GotoAsync(pageUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

                if (content is null || content.Status != 200)
                    throw new OperationException(localizationService.GetLocal("Scraper.PageProcessor.FailedToLoad", pageUrl.AbsoluteUri, content?.StatusText ?? "Unknown error"));

                string pageText = await content.TextAsync();

                return new PageSuccess(pageText ?? "<html>Failed page content</html>", pageUrl);
            });
}
