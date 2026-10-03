using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>
/// The application's main window. It only translates between controls and the collaborators that hold the logic: the status
/// reporter, readiness and the grouped menu actions.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly StatusReporter status;
    private readonly ApplicationReadiness readiness;
    private readonly MainWindowActions actions;

    public MainWindow(StatusReporter status, ApplicationReadiness readiness, MainWindowActions actions, ImageDisplayCoordinator imageDisplayCoordinator)
    {
        InitializeComponent();
        this.status = status;
        this.readiness = readiness;
        this.actions = actions;
        status.RefreshRequired += (_, _) => Dispatcher.UIThread.Post(RefreshStatusText);
        readiness.Changed += (_, _) => Dispatcher.UIThread.Post(UpdateControls);
        ImagePreview.Attach(imageDisplayCoordinator);
        Loaded += async (_, _) => await InitialiseAsync();
        UpdateControls();
    }

    public async void ImportConfiguration(object? sender, RoutedEventArgs eventArgs) => await actions.Configuration.ImportConfigurationAsync(new WindowDialogHost(this));

    public async void ExportConfiguration(object? sender, RoutedEventArgs eventArgs) => await actions.Configuration.ExportConfigurationAsync(new WindowDialogHost(this), ApiKeyExport.Exclude);

    public async void ExportConfigurationWithApiKeys(object? sender, RoutedEventArgs eventArgs) => await actions.Configuration.ExportConfigurationAsync(new WindowDialogHost(this), ApiKeyExport.Include);

    public async void EditConfiguration(object? sender, RoutedEventArgs eventArgs) => await actions.Configuration.EditConfigurationAsync(new WindowDialogHost(this));

    public async void EditTags(object? sender, RoutedEventArgs eventArgs) => await actions.Tags.EditTagsAsync(new WindowDialogHost(this));

    public async void ClearDownloads(object? sender, RoutedEventArgs eventArgs) => await actions.Scrape.ClearDownloadsAsync(new WindowDialogHost(this));

    public async void RunScraper(object? sender, RoutedEventArgs eventArgs) => await actions.Scrape.RunScraperAsync(new ScrapeSelection(HotWallpapersSwitch.IsChecked == true, TopWallpapersSwitch.IsChecked == true, CategoriesSwitch.IsChecked == true));

    public void CancelOperation(object? sender, RoutedEventArgs eventArgs) => actions.Scrape.CancelOperation();

    public void Exit(object? sender, RoutedEventArgs eventArgs) => Close();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F4 && e.KeyModifiers == KeyModifiers.Alt)
        {
            Close();

            return;
        }

        base.OnKeyDown(e);
    }

    private async Task InitialiseAsync() => await readiness.InitialiseAsync();

    private void UpdateControls()
    {
        ImportConfigurationMenuItem.IsEnabled = readiness.CanOperate;
        ExportConfigurationMenuItem.IsEnabled = readiness.CanOperate;
        ExportConfigurationWithApiKeysMenuItem.IsEnabled = readiness.CanOperate;
        EditConfigurationMenuItem.IsEnabled = readiness.CanOperate;
        EditTagsMenuItem.IsEnabled = readiness.CanOperate;
        ClearDownloadsMenuItem.IsEnabled = readiness.CanOperate;
        RunScraperButton.IsEnabled = readiness.CanRunScraper;
        CancelButton.IsEnabled = readiness.IsOperationRunning;
    }

    private void RefreshStatusText()
    {
        StatusTextBlock.Text = status.Text;
        StatusScrollViewer.ScrollToEnd();
    }
}
