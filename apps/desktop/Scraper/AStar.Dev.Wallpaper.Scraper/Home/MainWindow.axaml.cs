using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Home;

[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    private IDisposable? thumbnailSubscription;
    private IDisposable? categorySkippedSubscription;

    // Parameterless constructor is required by the XAML previewer only; the app resolves the DI constructor.
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel, IWallpaperThumbnailFeed thumbnailFeed) : this()
    {
        DataContext = viewModel;
        Width = viewModel.WindowWidth;
        Height = viewModel.WindowHeight;

        viewModel.ConfirmScrape.RegisterHandler(async context =>
            context.SetOutput(await new ConfirmDialog(context.Input).ShowDialog<bool?>(this) ?? false));

        viewModel.OpenEditor.RegisterHandler(async context =>
        {
            await new EntityEditorWindow { DataContext = context.Input }.ShowDialog(this);
            context.SetOutput(Unit.Default);
        });

        viewModel.WhenAnyValue(vm => vm.StatusText)
            .Subscribe(_ => Dispatcher.UIThread.Post(StatusScroller.ScrollToEnd, DispatcherPriority.Background));

        thumbnailSubscription = thumbnailFeed.Thumbnails
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(payload =>
            {
                ThumbnailImage.Source = new Bitmap(new MemoryStream(payload.Bytes));
                viewModel.ThumbnailCategoryName = payload.CategoryName;
                viewModel.ThumbnailTags = string.Join(", ", payload.Tags);
            });

        categorySkippedSubscription = thumbnailFeed.CategorySkipped
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(categoryName =>
            {
                ThumbnailImage.Source = null;
                viewModel.ThumbnailCategoryName = $"Skipping Category: {categoryName} as already fully downloaded";
                viewModel.ThumbnailTags = string.Empty;
            });

        Closed += (_, _) =>
        {
            thumbnailSubscription?.Dispose();
            categorySkippedSubscription?.Dispose();
        };
    }
}