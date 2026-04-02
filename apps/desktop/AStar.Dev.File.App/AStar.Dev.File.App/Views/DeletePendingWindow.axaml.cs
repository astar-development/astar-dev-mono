using AStar.Dev.File.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AStar.Dev.File.App.Views;

public partial class DeletePendingWindow : Window
{
    public DeletePendingWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is DeletePendingViewModel vm)
        {
            vm.ViewFileRequested += OnViewFileRequested;
        }
    }

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
}
