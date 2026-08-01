using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;

public partial class ScrapeConfigurationView : Window, IDisposable
{
    private readonly ScrapeConfigurationService scrapeConfigurationService;
    private readonly IImportExportService importExportService;
    private readonly ILogger logger;
    private CancellationTokenSource? cts;

    public ScrapeConfigurationView(ScrapeConfigurationViewModel viewModel, ScrapeConfigurationService scrapeConfigurationService, IImportExportService importExportService, ILogger logger)
    {
        this.scrapeConfigurationService = scrapeConfigurationService;
        this.importExportService = importExportService;
        this.logger = logger;
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        await ((ScrapeConfigurationViewModel)DataContext!).LoadAsync();
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        cts?.Dispose();
        if (DataContext is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    private async void OnExportScrapeConfigClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ = await ScrapeSiteWorkflowDecision.DecideAsync(
                ResetCancellationTokenSource().Tap(onSuccess: DisableControlsAndClearStatus, onFailure: LogSetupFailure),
                ct => Result.Success<CancellationToken, string>(ct)
                    .Tap(_ => { logger.Information("Exporting scrape configuration..."); ViewModel.UpdateStatus("Exporting scrape configuration..."); })
                    .MapAsync(_ => scrapeConfigurationService.ExportScrapeConfigurationAsync(ct))
                    .Tap(importExportService.ExportScrapeConfigurationToFile)
                    .TapAsync(_ => { logger.Information("Scrape configuration export completed..."); ViewModel.UpdateStatus("Export completed."); }));
        }
        finally
        {
            ResetUI();
        }
    }

    private async void OnImportScrapeConfigClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ = await ScrapeSiteWorkflowDecision.DecideAsync(
                ResetCancellationTokenSource().Tap(onSuccess: DisableControlsAndClearStatus, onFailure: LogSetupFailure),
                ct => Result.Success<CancellationToken, string>(ct)
                    .Tap(_ => { logger.Information("Importing scrape configuration..."); ViewModel.UpdateStatus("Importing scrape configuration..."); })
                    .Bind(_ => importExportService.ImportScrapeConfigurationFromFile().ToStringError())
                    .MapAsync(entity => scrapeConfigurationService.ImportScrapeConfigurationAsync(entity, ct))
                    .TapAsync(_ => { logger.Information("Scrape configuration import completed..."); ViewModel.UpdateStatus("Import completed."); }));
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
        ViewModel.UpdateStatus($"Error: {exception.Message}");
    }

    private void DisableControlsAndClearStatus(CancellationToken ct)
    {
        ExportScrapeConfigButton.IsEnabled = false;
        ImportScrapeConfigButton.IsEnabled = false;
        ViewModel.UpdateStatus(string.Empty);
    }

    private void ResetUI()
        => Dispatcher.UIThread.InvokeAsync(() =>
            {
                ExportScrapeConfigButton.IsEnabled = true;
                ImportScrapeConfigButton.IsEnabled = true;
                cts?.Dispose();
                cts = null;
            });

    private ScrapeConfigurationViewModel ViewModel => (ScrapeConfigurationViewModel)DataContext!;

    public void Dispose()
    {
        cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
