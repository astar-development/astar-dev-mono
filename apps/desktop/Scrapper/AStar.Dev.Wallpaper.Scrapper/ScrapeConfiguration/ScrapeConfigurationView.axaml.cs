using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.FunctionalParadigm;
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
        if(DataContext is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    private async void OnExportScrapeConfigClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    logger.Error(ex, "Failed to reset cancellation token source");
                    ViewModel.UpdateStatus($"Error: {ex.Message}");
                    return ex.Message;
                }
            )
            .Tap(_ => { logger.Information("Exporting scrape configuration..."); ViewModel.UpdateStatus("Exporting scrape configuration..."); })
            .MapAsync(_ => scrapeConfigurationService.ExportScrapeConfigurationAsync(cts!.Token))
            .Tap(importExportService.ExportScrapeConfigurationToFile)
            .TapAsync(_ => { logger.Information("Scrape configuration export completed..."); ViewModel.UpdateStatus("Export completed."); })
            .EnsureAsync(() => ResetUI());

    private async void OnImportScrapeConfigClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    logger.Error(ex, "Failed to reset cancellation token source");
                    ViewModel.UpdateStatus($"Error: {ex.Message}");
                    return ex.Message;
                }
            )
            .Tap(_ => { logger.Information("Importing scrape configuration..."); ViewModel.UpdateStatus("Importing scrape configuration..."); })
            .Bind(_ => importExportService.ImportScrapeConfigurationFromFile())
            .MapAsync(entity => scrapeConfigurationService.ImportScrapeConfigurationAsync(entity, cts!.Token))
            .TapAsync(_ => { logger.Information("Scrape configuration import completed..."); ViewModel.UpdateStatus("Import completed."); })
            .EnsureAsync(() => ResetUI());

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();

        return cts.Token;
    }

    private Result<CancellationToken, string> DisableControlsAndClearStatus(CancellationToken ct = default)
    {
        ExportScrapeConfigButton.IsEnabled = false;
        ImportScrapeConfigButton.IsEnabled = false;
        ViewModel.UpdateStatus(string.Empty);

        return ct;
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
