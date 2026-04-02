using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.ViewModels;

namespace AStar.Dev.File.App.Tests.Unit;

public class ViewWindowViewModelTests
{
    private static ScannedFileDisplayItem MakeImageItem() =>
        new(new ScannedFile
        {
            RootPath = "/",
            FolderPath = "/photos",
            FileName = "test.jpg",
            FullPath = "/photos/test.jpg",
            FileType = FileType.Image,
            LastModified = DateTime.UtcNow
        });

    private static ScannedFileDisplayItem MakeDocumentItem() =>
        new(new ScannedFile
        {
            RootPath = "/",
            FolderPath = "/docs",
            FileName = "report.pdf",
            FullPath = "/docs/report.pdf",
            FileType = FileType.Document,
            LastModified = DateTime.UtcNow
        });

    [Fact]
    public void NonImageFile_UsesMinimumWindowDimensions()
    {
        var sut = new ViewWindowViewModel(MakeDocumentItem(), 0, 0);
        sut.WindowWidth.ShouldBe(ViewWindowViewModel.MinWidth);
        sut.WindowHeight.ShouldBe(ViewWindowViewModel.MinHeight);
        sut.ImageDisplayWidth.ShouldBe(0);
        sut.ImageDisplayHeight.ShouldBe(0);
    }

    [Fact]
    public void SmallImage_UsesNaturalSize()
    {
        var sut = new ViewWindowViewModel(MakeImageItem(), 1920, 1080);
        sut.ImageDisplayWidth.ShouldBe(1920.0);
        sut.ImageDisplayHeight.ShouldBe(1080.0);
        sut.WindowWidth.ShouldBe(1920.0 + ViewWindowViewModel.DetailsPanelWidth);
        sut.WindowHeight.ShouldBe(1080.0);
    }

    [Fact]
    public void ImageAtExactMaxSize_UsesNaturalSize()
    {
        int maxImgW = ViewWindowViewModel.MaxWindowWidth - ViewWindowViewModel.DetailsPanelWidth;
        int maxImgH = ViewWindowViewModel.MaxWindowHeight;

        var sut = new ViewWindowViewModel(MakeImageItem(), maxImgW, maxImgH);

        sut.ImageDisplayWidth.ShouldBe(maxImgW);
        sut.ImageDisplayHeight.ShouldBe(maxImgH);
        sut.WindowWidth.ShouldBe(ViewWindowViewModel.MaxWindowWidth);
        sut.WindowHeight.ShouldBe(ViewWindowViewModel.MaxWindowHeight);
    }

    [Fact]
    public void OversizedImage_IsScaledProportionallyByHeight()
    {
        // 3840x2160 (4K) — constrained by height (scale = 1440/2160 = 2/3)
        var sut = new ViewWindowViewModel(MakeImageItem(), 3840, 2160);
        sut.ImageDisplayWidth.ShouldBe(2560.0, 0.01);
        sut.ImageDisplayHeight.ShouldBe(1440.0, 0.01);
        sut.WindowWidth.ShouldBe(2560.0 + ViewWindowViewModel.DetailsPanelWidth, 0.01);
        sut.WindowHeight.ShouldBe(1440.0, 0.01);
    }

    [Fact]
    public void VeryWideImage_IsScaledProportionallyByWidth_AndWindowHeightClampsToMinimum()
    {
        // 5000x500 — constrained by width (scale = 3140/5000 = 0.628)
        // ImageDisplayHeight = 500 * 0.628 = 314 < MinHeight → WindowHeight = MinHeight
        var sut = new ViewWindowViewModel(MakeImageItem(), 5000, 500);
        sut.ImageDisplayWidth.ShouldBe(3140.0, 0.01);
        sut.ImageDisplayHeight.ShouldBe(314.0, 0.01);
        sut.WindowWidth.ShouldBe(3440.0, 0.01);
        sut.WindowHeight.ShouldBe(ViewWindowViewModel.MinHeight);
    }
}
