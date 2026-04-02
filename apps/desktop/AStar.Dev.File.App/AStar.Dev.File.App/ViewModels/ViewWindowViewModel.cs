using System;

namespace AStar.Dev.File.App.ViewModels;

public class ViewWindowViewModel
{
    public const int MaxWindowWidth = 3440;
    public const int MaxWindowHeight = 1440;
    public const int DetailsPanelWidth = 300;
    public const int MinWidth = 600;
    public const int MinHeight = 400;

    public ScannedFileDisplayItem File { get; }

    public double ImageDisplayWidth { get; }
    public double ImageDisplayHeight { get; }
    public double WindowWidth { get; }
    public double WindowHeight { get; }

    public ViewWindowViewModel(ScannedFileDisplayItem file, int imagePixelWidth, int imagePixelHeight)
    {
        File = file;

        if (!file.IsImage || imagePixelWidth <= 0 || imagePixelHeight <= 0)
        {
            ImageDisplayWidth = 0;
            ImageDisplayHeight = 0;
            WindowWidth = MinWidth;
            WindowHeight = MinHeight;
            return;
        }

        int maxImgWidth = MaxWindowWidth - DetailsPanelWidth;
        int maxImgHeight = MaxWindowHeight;

        if (imagePixelWidth <= maxImgWidth && imagePixelHeight <= maxImgHeight)
        {
            ImageDisplayWidth = imagePixelWidth;
            ImageDisplayHeight = imagePixelHeight;
        }
        else
        {
            double scale = Math.Min((double)maxImgWidth / imagePixelWidth,
                                    (double)maxImgHeight / imagePixelHeight);
            ImageDisplayWidth = imagePixelWidth * scale;
            ImageDisplayHeight = imagePixelHeight * scale;
        }

        WindowWidth = Math.Max(MinWidth, ImageDisplayWidth + DetailsPanelWidth);
        WindowHeight = Math.Max(MinHeight, ImageDisplayHeight);
    }
}
