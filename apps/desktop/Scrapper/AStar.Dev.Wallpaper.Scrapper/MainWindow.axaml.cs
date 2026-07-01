using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Classifications;
using AStar.Dev.Wallpaper.Scrapper.Tags;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using AStar.Dev.Wallpaper.Scrapper.Dialogs;
using Serilog;
using AStar.Dev.Wallpaper.Scrapper.Services;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class MainWindow : Window, IDisposable
{
    private readonly Func<ScrapeConfigurationView> scrapeConfigViewFactory;
    private readonly Func<ClassificationsView> classificationsViewFactory;
    private readonly Func<TagsView> tagsViewFactory;
    private readonly ILogger logger;
    private readonly SearchWorkflowFunctional searchWorkflowFunctional;
    private readonly LogBroadcaster logBroadcaster;
    private readonly ImageBroadcaster imageBroadcaster;
    private readonly IDatabaseResetService databaseResetService;
    private CancellationTokenSource? cts;

    public MainWindow(Func<ScrapeConfigurationView> scrapeConfigViewFactory, Func<ClassificationsView> classificationsViewFactory, Func<TagsView> tagsViewFactory, SearchWorkflowFunctional searchWorkflowFunctional, ILogger logger, LogBroadcaster logBroadcaster, ImageBroadcaster imageBroadcaster, IDatabaseResetService databaseResetService)
    {
        this.scrapeConfigViewFactory = scrapeConfigViewFactory;
        this.classificationsViewFactory = classificationsViewFactory;
        this.tagsViewFactory = tagsViewFactory;
        this.logger = logger;
        this.searchWorkflowFunctional = searchWorkflowFunctional;
        this.logBroadcaster = logBroadcaster;
        this.imageBroadcaster = imageBroadcaster;
        this.databaseResetService = databaseResetService;
        logBroadcaster.MessageLogged += UpdateStatus;
        imageBroadcaster.ImageSaved += UpdateThumbnail;
        InitializeComponent();
        ThumbnailImage.Source = CreatePlaceholderBitmap();
        Closed += (_, _) => cts?.Dispose();
    }

    private async void OnEditConfigurationClicked(object? sender, RoutedEventArgs e)
        => await scrapeConfigViewFactory().ShowDialog(this);

    private async void OnEditClassificationsClicked(object? sender, RoutedEventArgs e)
        => await classificationsViewFactory().ShowDialog(this);

    private async void OnEditTagsClicked(object? sender, RoutedEventArgs e)
        => await tagsViewFactory().ShowDialog(this);

    private async void OnResetDatabaseClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dbDialog = new ConfirmationDialog("This will reset search category progress and delete all file records. Continue?");
            var dbConfirmed = await dbDialog.ShowDialog<bool>(this);

            if (!dbConfirmed)
                return;

            try
            {
                await databaseResetService.ResetAsync(CancellationToken.None);
                UpdateStatus("Database reset completed successfully.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Database reset failed");
                UpdateStatus($"Database reset failed: {ex.Message}");
                return;
            }

            var fileDialog = new ConfirmationDialog("This will permanently delete all downloaded files from the save directory. Continue?");
            var fileConfirmed = await fileDialog.ShowDialog<bool>(this);

            if (!fileConfirmed)
                return;

            try
            {
                await databaseResetService.DeleteSaveDirectoryAsync(CancellationToken.None);
                UpdateStatus("Save directory deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Save directory deletion failed");
                UpdateStatus($"Save directory deletion failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error during database reset");
            UpdateStatus($"Unexpected error: {ex.Message}");
        }
    }

    private async void OnScrapeSiteFunctionalClicked(object? sender, RoutedEventArgs e)
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
            .Tap(_ => logger.Information("Configuring Playwright..."))
            .Tap(_ => logger.Information("Starting scrape..."))
            .BindAsync(page => searchWorkflowFunctional.RunAsync(logger, cts!.Token))
            .TapAsync(_ => logger.Information("Scrape completed..."))
            .EnsureAsync(() => ResetUI());

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();

        return cts.Token;
    }

    private Result<CancellationToken, string> DisableControlsAndClearStatus(CancellationToken ct = default)
    {
        ScrapeSiteNewButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        StatusLabel.Text = string.Empty;

        return ct;
    }

    private void ResetUI()
        => Dispatcher.UIThread.InvokeAsync(() =>
            {
                ScrapeSiteNewButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
                cts?.Dispose();
                cts = new();
            });

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => cts?.Cancel();

    private void UpdateStatus(string message)
        => Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text += message + Environment.NewLine;
            StatusScroller.ScrollToEnd();
        });

    private void UpdateThumbnail(string imagePath)
        => Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                ThumbnailImage.Source = CreateRoundedBitmap(imagePath);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to load thumbnail for {imagePath}", imagePath);
            }
        });

    private static Bitmap CreateRoundedBitmap(string imagePath)
    {
        using var original = SKBitmap.Decode(imagePath);
        using var surface = SKSurface.Create(new SKImageInfo(500, 500, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var scale = Math.Min(500f / original.Width, 500f / original.Height);
        var drawWidth = original.Width * scale;
        var drawHeight = original.Height * scale;
        var offsetX = (500f - drawWidth) / 2f;
        var offsetY = (500f - drawHeight) / 2f;
        var destRect = new SKRect(offsetX, offsetY, offsetX + drawWidth, offsetY + drawHeight);

        using var clipPath = new SKPath();
        clipPath.AddRoundRect(destRect, 20, 20);
        canvas.ClipPath(clipPath, antialias: true);

        var srcRect = new SKRect(0, 0, original.Width, original.Height);
        canvas.DrawBitmap(original, srcRect, destRect);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());

        return new Bitmap(ms);
    }

    private static Bitmap CreatePlaceholderBitmap()
    {
        using var surface = SKSurface.Create(new SKImageInfo(500, 500, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var bgPaint = new SKPaint { Color = new SKColor(60, 60, 60), IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(0, 0, 500, 500), 20), bgPaint);

        using var font = new SKFont(SKTypeface.Default, 28);
        using var textPaint = new SKPaint { Color = SKColors.LightGray, IsAntialias = true };
        canvas.DrawText("No image downloaded yet", 250, 260, SKTextAlign.Center, font, textPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());

        return new Bitmap(ms);
    }

    public void Dispose()
    {
        logBroadcaster.MessageLogged -= UpdateStatus;
        imageBroadcaster.ImageSaved -= UpdateThumbnail;
        cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
