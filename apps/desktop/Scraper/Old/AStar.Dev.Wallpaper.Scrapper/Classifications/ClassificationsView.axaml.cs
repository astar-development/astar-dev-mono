using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Classifications;

public partial class ClassificationsView : Window, IDisposable
{
    private readonly FileClassificationService fileClassificationService;
    private readonly IImportExportService importExportService;
    private readonly ILogger logger;
    private readonly LogBroadcaster logBroadcaster;
    private CancellationTokenSource? cts;

    public ClassificationsView(FileClassificationService fileClassificationService, IImportExportService importExportService, ILogger logger, LogBroadcaster logBroadcaster)
    {
        this.fileClassificationService = fileClassificationService;
        this.importExportService = importExportService;
        this.logger = logger;
        this.logBroadcaster = logBroadcaster;
        logBroadcaster.MessageLogged += UpdateStatus;
        InitializeComponent();
        Closed += (_, _) => cts?.Dispose();
    }

    private async void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ = await ScrapeSiteWorkflowDecision.DecideAsync(
                ResetCancellationTokenSource().Tap(onSuccess: DisableControlsAndClearStatus, onFailure: LogSetupFailure),
                ct => Result.Success<CancellationToken, string>(ct)
                    .Tap(_ => logger.Information("Exporting classifications..."))
                    .MapAsync(_ => fileClassificationService.ExportClassificationsAsync(ct))
                    .Tap(importExportService.ExportFileClassificationsToFile)
                    .TapAsync(_ => logger.Information("Export completed...")));
        }
        finally
        {
            ResetUI();
        }
    }

    private async void OnImportClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ = await ScrapeSiteWorkflowDecision.DecideAsync(
                ResetCancellationTokenSource().Tap(onSuccess: DisableControlsAndClearStatus, onFailure: LogSetupFailure),
                ct => Result.Success<CancellationToken, string>(ct)
                    .Tap(_ => logger.Information("Importing classifications..."))
                    .Bind(_ => importExportService.ImportFileClassificationsFromFile().ToStringError())
                    .MapAsync(classifications => fileClassificationService.ImportClassificationsAsync(classifications, ct))
                    .TapAsync(_ => logger.Information("Import completed...")));
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
        ExportButton.IsEnabled = false;
        ImportButton.IsEnabled = false;
        StatusLabel.Text = string.Empty;
    }

    private void ResetUI()
        => Dispatcher.UIThread.InvokeAsync(() =>
            {
                ExportButton.IsEnabled = true;
                ImportButton.IsEnabled = true;
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
