using AStar.Dev.File.App.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.Specialized;

namespace AStar.Dev.File.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        vm.StatusMessages.CollectionChanged += OnStatusMessagesChanged;
        vm.ViewFileRequested += OnViewFileRequested;
        vm.OpenDeleteWindowRequested += OnOpenDeleteWindowRequested;
    }

    private void OnStatusMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e) => StatusScrollViewer.ScrollToEnd();

    private void OnViewFileRequested(ScannedFileDisplayItem item)
    {
        int imgW = 0, imgH = 0;

        if (item.IsImage && global::System.IO.File.Exists(item.FullPath))
        {
            try
            {
                using var bmp = new Bitmap(item.FullPath);
                imgW = bmp.PixelSize.Width;
                imgH = bmp.PixelSize.Height;
            }
            catch { }
        }

        var vm = new ViewWindowViewModel(item, imgW, imgH);
        new ViewWindow { DataContext = vm }.Show();
    }

    private void OnOpenDeleteWindowRequested()
    {
        if (Application.Current is App app)
        {
            var vm = app.GetService<DeletePendingViewModel>();
            new DeletePendingWindow { DataContext = vm }.Show();
        }
    }
}
