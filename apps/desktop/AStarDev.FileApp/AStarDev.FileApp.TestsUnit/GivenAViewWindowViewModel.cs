using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.ViewModels;

namespace AStar.Dev.File.App.TestsUnit;

public class GivenAViewWindowViewModel
{
    [Fact]
    public void when_constructed_with_a_non_image_file_then_uses_minimum_window_dimensions()
    {
        var sut = new ViewWindowViewModel(MakeDocumentItem(), 0, 0);

        sut.WindowWidth.ShouldBe(ViewWindowViewModel.MinWidth);
        sut.WindowHeight.ShouldBe(ViewWindowViewModel.MinHeight);
        sut.ImageDisplayWidth.ShouldBe(0);
        sut.ImageDisplayHeight.ShouldBe(0);
    }

    [Fact]
    public void when_constructed_with_a_small_image_then_uses_natural_size()
    {
        var sut = new ViewWindowViewModel(MakeImageItem(), 1920, 1080);

        sut.ImageDisplayWidth.ShouldBe(1920.0);
        sut.ImageDisplayHeight.ShouldBe(1080.0);
        sut.WindowWidth.ShouldBe(1920.0 + ViewWindowViewModel.DetailsPanelWidth);
        sut.WindowHeight.ShouldBe(1080.0);
    }

    [Fact]
    public void when_constructed_with_an_image_at_exact_max_size_then_uses_natural_size()
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
    public void when_constructed_with_an_oversized_image_then_is_scaled_proportionally_by_height()
    {
        var sut = new ViewWindowViewModel(MakeImageItem(), 3840, 2160);

        sut.ImageDisplayWidth.ShouldBe(2560.0, 0.01);
        sut.ImageDisplayHeight.ShouldBe(1440.0, 0.01);
        sut.WindowWidth.ShouldBe(2560.0 + ViewWindowViewModel.DetailsPanelWidth, 0.01);
        sut.WindowHeight.ShouldBe(1440.0, 0.01);
    }

    [Fact]
    public void when_constructed_with_a_very_wide_image_then_is_scaled_proportionally_by_width_and_window_height_clamps_to_minimum()
    {
        var sut = new ViewWindowViewModel(MakeImageItem(), 5000, 500);

        sut.ImageDisplayWidth.ShouldBe(3140.0, 0.01);
        sut.ImageDisplayHeight.ShouldBe(314.0, 0.01);
        sut.WindowWidth.ShouldBe(3440.0, 0.01);
        sut.WindowHeight.ShouldBe(ViewWindowViewModel.MinHeight);
    }

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
}
