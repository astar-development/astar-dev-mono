using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tags;

public partial class TagsView : Window, IDisposable
{
    private readonly IScrapedTagService scrapedTagService;
    private readonly IImportExportService importExportService;
    private readonly ILogger logger;
    private readonly LogBroadcaster logBroadcaster;
    private CancellationTokenSource? cts;

    public TagsView(IScrapedTagService scrapedTagService, IImportExportService importExportService, ILogger logger, LogBroadcaster logBroadcaster)
    {
        this.scrapedTagService = scrapedTagService;
        this.importExportService = importExportService;
        this.logger = logger;
        this.logBroadcaster = logBroadcaster;
        logBroadcaster.MessageLogged += UpdateStatus;
        InitializeComponent();
        Closed += (_, _) => cts?.Dispose();
    }

    private async void OnExportTagsClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    logger.Error(ex, "Failed to reset cancellation token source");
                    UpdateStatus($"Error: {ex.Message}");
                    return ex.Message;
                }
            )
            .Tap(_ => logger.Information("Exporting tags..."))
            .MapAsync(_ => scrapedTagService.ExportScrapedTagsAsync(cts!.Token))
            .Tap(importExportService.ExportScrapedTagsToFile)
            .TapAsync(_ => logger.Information("Tag export completed..."))
            .EnsureAsync(() => ResetUI());

    private async void OnImportTagsClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    logger.Error(ex, "Failed to reset cancellation token source");
                    return ex.Message;
                }
            )
            .Tap(_ => logger.Information("Importing tags..."))
            .Bind(_ => importExportService.ImportScrapedTagsFromFile())
            .MapAsync(tags => scrapedTagService.ImportScrapedTagsAsync(tags, cts!.Token))
            .TapAsync(_ => logger.Information("Tag import completed..."))
            .EnsureAsync(() => ResetUI());

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();

        return cts.Token;
    }

    private Result<CancellationToken, string> DisableControlsAndClearStatus(CancellationToken ct = default)
    {
        ExportTagsButton.IsEnabled = false;
        ImportTagsButton.IsEnabled = false;
        StatusLabel.Text = string.Empty;

        return ct;
    }

    private void ResetUI()
        => Dispatcher.UIThread.InvokeAsync(() =>
            {
                ExportTagsButton.IsEnabled = true;
                ImportTagsButton.IsEnabled = true;
                cts?.Dispose();
                cts = null;
            });

    private void UpdateStatus(string message)
        => Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text += message + Environment.NewLine;
            StatusScroller.ScrollToEnd();
        });

    public void Dispose()
    {
        logBroadcaster.MessageLogged -= UpdateStatus;
        cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
