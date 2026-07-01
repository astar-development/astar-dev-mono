using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
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
            .Tap(_ => logger.Information("Exporting classifications..."))
            .MapAsync(_ => fileClassificationService.ExportClassificationsAsync(cts!.Token))
            .Tap(importExportService.ExportFileClassificationsToFile)
            .TapAsync(_ => logger.Information("Export completed..."))
            .EnsureAsync(() => ResetUI());

    private async void OnImportClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    logger.Error(ex, "Failed to reset cancellation token source");
                    return ex.Message;
                }
            )
            .Tap(_ => logger.Information("Importing classifications..."))
            .Bind(_ => importExportService.ImportFileClassificationsFromFile())
            .MapAsync(classifications => fileClassificationService.ImportClassificationsAsync(classifications, cts!.Token))
            .TapAsync(_ => logger.Information("Import completed..."))
            .EnsureAsync(() => ResetUI());

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();

        return cts.Token;
    }

    private Result<CancellationToken, string> DisableControlsAndClearStatus(CancellationToken ct = default)
    {
        ExportButton.IsEnabled = false;
        ImportButton.IsEnabled = false;
        StatusLabel.Text = string.Empty;

        return ct;
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
