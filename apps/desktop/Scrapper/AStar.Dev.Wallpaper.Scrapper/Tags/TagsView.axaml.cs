using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
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
    {
        try
        {
            _ = await ScrapeSiteWorkflowDecision.DecideAsync(
                ResetCancellationTokenSource().Tap(onSuccess: DisableControlsAndClearStatus, onFailure: LogSetupFailure),
                ct => Result.Success<CancellationToken, string>(ct)
                    .Tap(_ => logger.Information("Exporting tags..."))
                    .MapAsync(_ => scrapedTagService.ExportScrapedTagsAsync(ct))
                    .Tap(importExportService.ExportScrapedTagsToFile)
                    .TapAsync(_ => logger.Information("Tag export completed...")));
        }
        finally
        {
            ResetUI();
        }
    }

    private async void OnImportTagsClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ = await ScrapeSiteWorkflowDecision.DecideAsync(
                ResetCancellationTokenSource().Tap(onSuccess: DisableControlsAndClearStatus, onFailure: LogSetupFailure),
                ct => Result.Success<CancellationToken, string>(ct)
                    .Tap(_ => logger.Information("Importing tags..."))
                    .Bind(_ => importExportService.ImportScrapedTagsFromFile().ToStringError())
                    .MapAsync(tags => scrapedTagService.ImportScrapedTagsAsync(tags, ct))
                    .TapAsync(_ => logger.Information("Tag import completed...")));
        }
        finally
        {
            ResetUI();
        }
    }

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();

        return cts.Token;
    }

    private void LogSetupFailure(Exception exception)
    {
        logger.Error(exception, "Failed to reset cancellation token source");
        UpdateStatus($"Error: {exception.Message}");
    }

    private void DisableControlsAndClearStatus(CancellationToken ct)
    {
        ExportTagsButton.IsEnabled = false;
        ImportTagsButton.IsEnabled = false;
        StatusLabel.Text = string.Empty;
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
